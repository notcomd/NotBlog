using FileDev.Web.API.Grpc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Notcomd.Token.JWT.Extensions;

namespace Message.Web.API.Extensions;

/// <summary>
/// Message.Web.API 应用的依赖注入统一注册入口。
/// <para>
/// 设计说明：
/// - 本文件集中管理 Web 层（API / SignalR / gRPC 客户端）所有需要注入的服务；
/// - 领域层与基础设施层的服务由 <c>Message.Infrastructure.ServiceCollectionExtensions.AddMessageInfrastructure</c> 负责注册，
///   此处仅注册与 Web 宿主相关的服务；
/// - 每个服务均明确标注生命周期（Singleton / Scoped / Transient），遵循"易变依赖长生命周期、短生命周期依赖短生命周期"原则；
/// - API 端点通过 <c>[FromServices]</c> 特性注入依赖，避免在方法体内手工解析。
/// </para>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册 Message.Web.API 应用所需的服务。
    /// 应在 <c>AddMessageInfrastructure</c> 之后调用（依赖其注册的领域服务）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置</param>
    /// <param name="environment">宿主环境（用于开发环境 CORS 放宽）</param>
    public static IServiceCollection AddMessageWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // ═══ 实时通信（SignalR）═══
        // 生命周期：SignalR 服务由框架内部管理，Hub 实例为 Transient（每次调用新建），
        //           HubContext 为 Singleton。
        services.AddSignalR();

        RegisterAuthentication(services, configuration);
        RegisterFileStorageGrpc(services, configuration);
        RegisterApplicationServices(services);
        RegisterTurnCredentials(services, configuration);
        RegisterCors(services, configuration, environment);

        return services;
    }

    /// <summary>
    /// JWT 认证注册（Hub 双通道认证：JWT Bearer 优先 + X-User-Id 头回退）。
    /// </summary>
    private static void RegisterAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration.GetSection("JwtOptions"));

        // SignalR 的 WebSocket 请求无法携带自定义请求头，需从 query string 读取 access_token
        // （覆盖全部 Hub 路径：/MessageHub、/CommunityHub、/CallHub）
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // ⚠️ 保留 AddJwtAuthentication 挂接的 OnTokenValidated（黑名单/吊销检查）——
            // 整体 new JwtBearerEvents 会把该钩子覆盖掉（JWT 吊销在 Message 端失效）
            var blacklistCheck = options.Events?.OnTokenValidated;
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && IsHubPath(context.HttpContext.Request.Path))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = blacklistCheck ?? (_ => Task.CompletedTask)
            };
        });
    }

    /// <summary>判断路径是否为 SignalR Hub 端点（这些端点需从 query 读取 access_token）</summary>
    private static bool IsHubPath(PathString path)
        => path.StartsWithSegments("/MessageHub")
           || path.StartsWithSegments("/CommunityHub")
           || path.StartsWithSegments("/CallHub");

    /// <summary>
    /// 文件存储 gRPC 客户端注册（调用 FileDev.Web.API 的文件上传服务）。
    /// 生命周期：
    /// - 配置快照（IOptionsSnapshot）：Scoped；
    /// - gRPC 客户端（GrpcClientFactory 创建）：Scoped（每次请求新建，避免复用失效 Channel）；
    /// - <see cref="FileStorageGrpcClient"/>：Scoped（依赖 IOptionsSnapshot，生命周期需 ≤ Scoped）。
    /// </summary>
    private static void RegisterFileStorageGrpc(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageGrpcOptions>(
            configuration.GetSection(FileStorageGrpcOptions.SectionName));

        services.AddGrpcClient<FileStorage.FileStorageClient>(
            FileStorageGrpcClient.ClientName,
            options =>
            {
                // 优先使用 Aspire 服务发现解析 FileDev 服务地址（虚拟主机名 = 服务名 filedev-web-api）；
                // 脱离 AppHost 独立启动时，使用 appsettings 中 FileStorageGrpc:Address 作为兜底地址。
                var configuredAddress = configuration["FileStorageGrpc:Address"];
                options.Address = new Uri(string.IsNullOrWhiteSpace(configuredAddress)
                    ? $"https://{FileStorageGrpcClient.ClientName}"
                    : configuredAddress);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                // S-16：仅 DEBUG/开发环境允许跳过证书校验；Release 下使用系统默认证书校验
#if DEBUG
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#endif
            });

        services.AddScoped<IFileStorageGrpcClient, FileStorageGrpcClient>();
    }

    /// <summary>
    /// Web 应用层服务注册。
    /// 生命周期：
    /// - <see cref="MessageDeliveryService"/>：Scoped（依赖 Scoped 的 IConnectionManager，故注册为 Scoped）。
    /// </summary>
    private static void RegisterApplicationServices(IServiceCollection services)
    {
        // 消息实时推送服务（依赖 Scoped 的 IConnectionManager，故注册为 Scoped）
        services.AddScoped<MessageDeliveryService>();

        // 社区实时推送服务（圈子频道 circle:{id}，依赖 Singleton 的 IHubContext，注册为 Scoped 即可）
        services.AddScoped<CommunityDeliveryService>();

        // F-04：覆盖 SignalR 默认的 DefaultUserIdProvider（仅读 NameIdentifier），
        // 改为从 JWT Claim sub / user_guid / NameIdentifier 解析用户 ID，避免 Clients.User 推送落空。
        services.AddSingleton<IUserIdProvider, MessageUserIdProvider>();

        // 通话会话存储与状态机（Singleton：Redis 存储 + IHubContext 推送；连接查询经作用域工厂解析）
        services.AddSingleton<CallSessionStore>();
    }

    /// <summary>
    /// WebRTC TURN 限时凭证注册。
    /// 生命周期：<see cref="TurnCredentialService"/> 仅依赖只读配置快照，注册为 Singleton。
    /// </summary>
    private static void RegisterTurnCredentials(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TurnServiceOptions>(configuration.GetSection(TurnServiceOptions.SectionName));
        services.AddSingleton<TurnCredentialService>();
    }

    /// <summary>
    /// 跨域策略注册（S-15）：白名单来源，禁止 AllowAnyOrigin 与 AllowCredentials 共存。
    /// <para>
    /// 开发环境（Development）：自动放行任意本地回环来源（localhost / 127.0.0.1 / [::1] 任意端口），
    /// 兼容 MessageHubTester 测试页、Vite 前端等多种本地调试来源，并允许携带凭据；
    /// 非开发环境：维持白名单（CorsSettings:AllowedOrigins），空白名单拒绝一切跨域。
    /// </para>
    /// </summary>
    private static void RegisterCors(
        IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var corsOrigins = configuration.GetSection("CorsSettings:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (environment.IsDevelopment())
                {
                    // 本地回环来源任意端口放行 + 允许凭据（SetIsOriginAllowed 与 AllowCredentials 可共存）
                    policy.SetIsOriginAllowed(IsLoopbackOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else if (corsOrigins.Length == 0)
                {
                    // 白名单为空：仅允许同源（拒绝一切跨域来源，也不允许携带凭据）
                    policy.SetIsOriginAllowed(_ => false);
                }
                else
                {
                    policy.WithOrigins(corsOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                }
            });
        });
    }

    /// <summary>判断来源是否为本地回环地址（localhost / 127.0.0.1 / [::1]，任意端口）</summary>
    private static bool IsLoopbackOrigin(string origin)
        => Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
