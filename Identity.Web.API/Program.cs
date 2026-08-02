using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");

builder.AddRabbitMQClient("EventBus");

builder.Services.AddNotEmail(opt =>
{
    builder.Configuration.GetSection("NotEmail").Bind(opt);
    // 凭据外置（S-01）：SMTP 密码从环境变量读取
    if (string.IsNullOrEmpty(opt.Password))
        opt.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
});


#if DEBUG

// 凭据外置（S-01）：数据库口令从环境变量 IDENTITY_DB_PASSWORD 读取
builder.Services.AddNotBlogServices(
    DbConnectionStringResolver.Resolve(
        builder.Configuration.GetSection("DbContextOption").GetValue<string>("DbContextConnect"),
        "IDENTITY_DB_PASSWORD"),
    [..ReflectionHelper.GetAllReferencedAssemblies()]);

#else
builder.Services.AddNotBlogServices(
    builder.Configuration.GetConnectionString("IdentityPostgres")
    ?? throw new InvalidOperationException(
        "未配置数据库连接字符串：请设置环境变量 ConnectionStrings__IdentityPostgres。"),
    [..ReflectionHelper.GetAllReferencedAssemblies()]);
#endif
builder.Services.AddIdentityService(builder.Configuration.GetSection("JwtOptions"));

builder.Services.AddMigration<IdentityDbContext, IdentityDbSeeder>();

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

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

// 注册 OAuth 服务
// 注册 Github 认证服务并配置弹性策略
builder.Services.AddHttpClient<GithubAuthService>()
    .ConfigureHttpClient(client => { client.Timeout = TimeSpan.FromMinutes(2); })
    .AddResilienceHandler("github-resilience", bu =>
    {
        bu.AddTimeout(TimeSpan.FromMinutes(2));
        bu.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential
        });
    });


// 注册 Github 认证 DI 聚合
builder.Services.AddScoped<GithubAuthDI>();

var app = builder.Build();

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
// 注册 Github 认证 API
app.MapGroup("api/identity/git").GithubAuthApis();
// 注册 OAuth 端点
app.MapGroup("api/identity/auth").MapOAuthEndpoints();
//管理端点
app.MapGroup("api/identity/manger").MapUserManagerApi();
//头像上传端点
app.MapGroup("api/identity").MapAvatarApi();

app.MapControllers();

app.Run();