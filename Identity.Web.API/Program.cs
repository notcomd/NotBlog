

using Identity.Web.API.Resources;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

//builder.AddNpgsql("IdentityPostgres");

builder.AddCacheMemory("Redis");

builder.AddRabbitMQClient("EventBus");

// ═══ NotEmail：Outlook OAuth 2.0 发送验证码 ═══
// Outlook.com 已禁用 SMTP 密码认证，必须使用 OAuth 2.0（MSAL 设备代码流）
var notEmailCfg = builder.Configuration.GetSection("NotEmail");
var outlookTokenService = new OutlookTokenService(
    string.IsNullOrWhiteSpace(notEmailCfg["ClientId"])
        ? throw new InvalidOperationException(
            "未配置 Outlook OAuth 2.0 ClientId：请在 appsettings.json 的 NotEmail:ClientId 填写 Azure 应用 ID。")
        : notEmailCfg["ClientId"]!,
    notEmailCfg["TenantId"] ?? "consumers",
    string.IsNullOrWhiteSpace(notEmailCfg["CachePath"]) ? null : notEmailCfg["CachePath"]);

builder.Services.AddSingleton(outlookTokenService);

builder.Services.AddNotEmail(opt =>
{
    notEmailCfg.Bind(opt);
    opt.UseOAuth2 = true;
    // OAuth 2.0 Access Token 获取/刷新回调（首次运行需在浏览器授权一次）
    opt.AccessTokenCallback = async ct => await outlookTokenService.GetAccessTokenAsync(ct);
});


// 数据库（Aspire 版 AddNpgsqlDbContext，connectionName 语义，2026-08-17 切换）：
// 从 ConnectionStrings:IdentityPostgres 读连接串注册 IdentityDbContext，自动健康检查/遥测。
// 单服务模式（无该连接串）时从 DbContextOption:DbContextConnection 桥接。
if (builder.Configuration.GetConnectionString("IdentityPostgres") is null)
{
    Console.WriteLine("单个服务执行！");
    builder.Configuration["ConnectionStrings:IdentityPostgres"] =
        builder.Configuration.GetSection("DbContextOption").GetValue<string>("DbContextConnection")
        ?? throw new ArgumentNullException("数据库连接字符未配置");
}
else
{
    Console.WriteLine("Aspire服务执行！");
}

// 模块自动初始化（仓储/领域服务注册；原 AddNotBlogServices 拆分，DbContext 改用 Aspire 注册）
builder.Services.AddAutoAddInstance([.. ReflectionHelper.GetAllReferencedAssemblies()]);

// DbContext 注册（Aspire 版：连接名语义 + 自动健康检查/OpenTelemetry）
builder.AddNpgsqlDbContext<IdentityDbContext>("IdentityPostgres");
builder.Services.AddIdentityService(builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddMigration<IdentityDbContext, IdentityDbSeeder>();

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());
builder.Services.RemoveAbstractHandlerRegistrations(); // 移除抽象泛型基类 handler（NotMediator 自动注册未过滤抽象类，2026-08-17）

builder.Services.AddScoped<IdentityService>();

// ═══ gRPC 客户端注册（调用 FileDev 文件服务） ═══
builder.Services.AddGrpcClient<Identity.Web.API.Grpc.FileStorage.FileStorageClient>(o =>
{
    var grpcAddress = builder.Configuration["FileStorageGrpc:Address"]
        ?? "https://localhost:5002";
    o.Address = new Uri(grpcAddress);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
#if DEBUG
    // S-16：仅 DEBUG/开发环境允许自签名证书；Release 下使用系统默认证书校验
    handler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#endif
    return handler;
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

// ═══ EventBus 注册（通过 IConfiguration 配置驱动） ═══
// IConnectionFactory 来源：Aspire AddRabbitMQClient("EventBus") 或手动注册

// var hostName = eventBusCfg["HostName"] ?? "localhost";
// var userName = eventBusCfg["UserName"] ?? "guest";
// var password = eventBusCfg["Password"] ?? "guest";
// var port = int.TryParse(eventBusCfg["Port"], out var p) ? p : 5672;

// builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
// {
//     HostName = hostName,
//     UserName = userName,
//     Password = password,
//     Port = port
// });
var eventBusCfg = builder.Configuration.GetSection("EventBus");
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

// S-19：Outbox 反序列化声明——Identity 发布的集成事件若无本地 handler，
// 必须在此显式注册事件类型，否则 OutboxPublisher 会按"未找到事件类型"丢弃消息
builder.Services.Configure<EventBusSubscriptionInfo>(o =>
{
    o.EventTypes[nameof(RegisterByUserIntegrationEvent)] = typeof(RegisterByUserIntegrationEvent);
});

// ═══ Outbox（S-19）：Identity 作为首个启用 Outbox 落表并投递的事件源服务 ═══
// 发送事件前调用 IOutboxStore.StoreAsync 持久化，OutboxPublisher 后台投递；
// OutboxMessages 表随 IdentityDbContext 迁移自动创建。
builder.Services.AddOutbox<IdentityDbContext>(opt =>
{
    opt.PollingIntervalMs = builder.Configuration.GetValue("EventBus:Outbox:PollingIntervalMs", 5000);
    opt.BatchSize = builder.Configuration.GetValue("EventBus:Outbox:BatchSize", 20);
    opt.RetentionDays = builder.Configuration.GetValue("EventBus:Outbox:RetentionDays", 7);
});

// ═══ CORS（S-15）：白名单来源，禁止 AllowAnyOrigin 与 AllowCredentials 共存 ═══
var corsOrigins = builder.Configuration.GetSection("CorsSettings")
    .Get<CorsSettings>()?.AllowedOrigins ?? [];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length == 0)
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

// ═══ 授权策略（S-03）：管理端点仅允许 Root / 管理员 ═══
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(ctx =>
        ctx.User.FindAll(ClaimTypes.Role)
            .SelectMany(c => c.Value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(r => r is "Root" or "Administrator")));
});

// 配置 OAuth 选项
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuthOptions"));

// 凭据外置（S-01）：GitHub ClientSecret 从环境变量读取，缺失且已启用时报清晰错误
builder.Services.PostConfigure<OAuthOptions>(opt =>
{
    if (opt.GitHubOptions is { Enable: true } && string.IsNullOrEmpty(opt.GitHubOptions.ClientSecret))
    {
        opt.GitHubOptions.ClientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET")
            ?? throw new InvalidOperationException(
                "GitHub OAuth 已启用但未配置 ClientSecret：请在环境变量 GITHUB_CLIENT_SECRET 中设置。");
    }
});

// Microsoft OAuth：ClientId 缺省复用 NotEmail 的 Outlook 应用（公共客户端 + PKCE）；
// ClientSecret 可留空（走公共客户端 PKCE），也可通过环境变量 MICROSOFT_CLIENT_SECRET 提供（走机密客户端）。
builder.Services.PostConfigure<OAuthOptions>(opt =>
{
    var microsoft = opt.MicrosoftOptions;
    if (!microsoft.Enable)
        return;

    if (string.IsNullOrWhiteSpace(microsoft.ClientId))
    {
        microsoft.ClientId = notEmailCfg["ClientId"]
            ?? throw new InvalidOperationException(
                "Microsoft OAuth 未配置 ClientId：请在 OAuthOptions:MicrosoftOptions:ClientId 或 NotEmail:ClientId 中设置。");
    }

    if (string.IsNullOrWhiteSpace(microsoft.ClientSecret))
        microsoft.ClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET") ?? string.Empty;
});

// 凭据外置：微信 AppSecret 从环境变量读取（与 GitHub ClientSecret 同模式）
builder.Services.PostConfigure<OAuthOptions>(opt =>
{
    if (opt.WeChatOptions is { Enabled: true } && string.IsNullOrEmpty(opt.WeChatOptions.AppSecret))
    {
        opt.WeChatOptions.AppSecret = Environment.GetEnvironmentVariable("WECHAT_APP_SECRET")
            ?? throw new InvalidOperationException(
                "WeChat OAuth 已启用但未配置 AppSecret：请在环境变量 WECHAT_APP_SECRET 中设置。");
    }
});

// 凭据外置：QQ AppKey 从环境变量读取
builder.Services.PostConfigure<OAuthOptions>(opt =>
{
    if (opt.QQOptions is { Enabled: true } && string.IsNullOrEmpty(opt.QQOptions.AppKey))
    {
        opt.QQOptions.AppKey = Environment.GetEnvironmentVariable("QQ_APP_KEY")
            ?? throw new InvalidOperationException(
                "QQ OAuth 已启用但未配置 AppKey：请在环境变量 QQ_APP_KEY 中设置。");
    }
});

// 注册 OAuth 服务
// 注：GitHub 端点已统一到 /api/identity/auth/oauth/{provider}（OAuthApis），
// OAuthService 内部自行实现 GitHub 用户获取（GetGitHubUserInfoAsync）。

var app = builder.Build();

// 启动 Banner（ASCII 字符画）：原样输出到控制台，避免 logger 前缀破坏对齐；文件缺失/读取失败不影响启动
ResourcesBanner.PrintStartupBanner();


app.MapDefaultEndpoints();

app.UseNotBlogPipeline();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

//登入注册端点
app.MapGroup("api/identity/ready").NotMapIdentityApi();
// 权限映射端点（供网关启动时拉取）+ 权限 CRUD
app.MapGroup("api/identity/permission").MapPermissionApi();
// 角色组管理端点
app.MapGroup("api/identity/rolegroup").MapRoleGroupApi();
// 角色管理端点
app.MapGroup("api/identity/role").MapRoleApi();
// OAuth 客户端（NotClient）管理端点（AdminOnly）
app.MapGroup("api/identity/client").MapClientApi();
// 注册 OAuth 端点（统一支持 google / github / microsoft / wechat / qq 登录与回调）
app.MapGroup("api/identity/auth").MapOAuthEndpoints();
// 注册 OAuth 2.0 授权服务器端点（RFC 6749：NotClient 客户端注册体系）
app.MapGroup("api/identity/oauth").MapOAuthServerApi();
// 邮件验证码 RESTful 端点（公共访问：发送验证码 + 确认验证结果）
app.MapGroup("api/identity/ready").MapEmailVerificationApi();
//管理端点
app.MapGroup("api/identity/manger").MapUserManagerApi();
//头像上传端点
app.MapGroup("api/identity").MapAvatarApi();

// ═══ NotEmail 管理端点：无控制台环境下获取 Outlook OAuth 授权链接 ═══
// 调用后返回授权 URL 与代码，管理员在任意浏览器完成授权即可（需管理员权限）
app.MapGet("api/email/authorize", async (OutlookTokenService svc) =>
{
    var result = await svc.BeginDeviceCodeAsync();
    return Results.Ok(new
    {
        verification_url = result.VerificationUrl,
        user_code = result.UserCode,
        expires_in_seconds = (int)(result.ExpiresOn - DateTimeOffset.UtcNow).TotalSeconds,
        message = result.Message
    });
}).RequireAuthorization("AdminOnly");

app.MapControllers();

app.Run();