using FileDev.Web.API.Grpc;
using Message.Web.API.Grpc;
using Message.Web.API.Services;
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
    public static IServiceCollection AddMessageWebApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ═══ 实时通信（SignalR）═══
        // 生命周期：SignalR 服务由框架内部管理，Hub 实例为 Transient（每次调用新建），
        //           HubContext 为 Singleton。
        services.AddSignalR();

        RegisterAuthentication(services, configuration);
        RegisterFileStorageGrpc(services, configuration);
        RegisterApplicationServices(services);
        RegisterCors(services);

        return services;
    }

    /// <summary>
    /// JWT 认证注册（Hub 双通道认证：JWT Bearer 优先 + X-User-Id 头回退）。
    /// </summary>
    private static void RegisterAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration.GetSection("JwtOptions"));

        // SignalR 的 WebSocket 请求无法携带自定义请求头，需从 query string 读取 access_token
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments("/MessageHub"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
    }

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
                // 开发环境 FileDev 使用自签名开发证书，需跳过证书校验
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
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
    }

    /// <summary>
    /// 跨域策略注册（SignalR 长连接需要宽松的跨域配置）。
    /// </summary>
    private static void RegisterCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .SetIsOriginAllowed(_ => true);
            });
        });
    }
}
