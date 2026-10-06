
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

/// 数据库（Aspire 版 AddNpgsqlDbContext，connectionName 语义，2026-08-17 切换）：
/// 从 ConnectionStrings:VideoPostgres 读连接串注册 VideoDbContext，自动健康检查/遥测。
/// 单服务模式（无该连接串）时从 DbContextOption:DbContextConnection 桥接。
if (builder.Configuration.GetConnectionString("VideoPostgres") is null)
{
    Console.WriteLine("单个服务执行！");
    builder.Configuration["ConnectionStrings:VideoPostgres"] =
        builder.Configuration.GetSection("DbContextOption").GetValue<string>("DbContextConnection")
        ?? throw new ArgumentNullException("数据库连接字符未配置");
}
else
{
    Console.WriteLine("Aspire服务执行！");
}

/// 模块自动初始化（仓储/领域服务注册；原 AddNotBlogServices 拆分，DbContext 改用 Aspire 注册）
builder.AddNpgsqlDbContext<VideoDbContext>("VideoPostgres");

// CacheMemory (Redis) — Aspire-style registration
builder.AddCacheMemory("CacheMemory");

// Add Video domain and infrastructure services
var fileDevBaseUrl = builder.Configuration.GetValue<string>("FileDev:BaseUrl") ?? "http://localhost:5000";
builder.Services.AddVideoInfrastructure(fileDevBaseUrl);

// Apply ReviewContent configuration from appsettings.json
builder.Configuration.ConfigureReviewContentOptions();

// Configure gRPC client options
builder.Services.Configure<GrpcClientOptions>(
    builder.Configuration.GetSection(GrpcClientOptions.SectionName));

// FileDev gRPC 命名 HttpClient：ServiceDefaults 已为其注入服务发现（解析服务名 filedev-web-api）
// 与标准重试/日志管道；真正的 gRPC Channel 在 UploadVideoViaGrpcCommandHandler 内构建（需自定义消息大小上限）。
builder.Services.AddHttpClient(UploadVideoViaGrpcCommandHandler.FileDevGrpcHttpClientName);

// Add HTTP client for streaming proxy to FileDev
builder.Services.AddHttpClient("FileDevProxy", client =>
{
    client.BaseAddress = new Uri(fileDevBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(30);
});

// JWT 认证（S-07）：与 Identity 一致，从 JwtOptions 配置节读取，凭据外置
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<VideoServiceDI>();

// NotMediator with pipeline behaviors
builder.Services.AddNotMediator(typeof(Program).Assembly);

// 配置 EventBus（通过 IConfiguration 配置驱动）
// IConnectionFactory 来源：Aspire WithReference(rabbitmq) 注入 ConnectionStrings:EventBus → AddRabbitMQClient；
// 单机（无该连接串）→ 从 EventBus 配置节手动构建，否则 RabbitMqConnection 激活失败。
// 采用运行时判断（对齐 FileDev/Identity/Message）：避免 Debug 配置下经 AppHost 启动时误连本机 5672。
var eventBusCfg = builder.Configuration.GetSection("EventBus");
if (builder.Configuration.GetConnectionString("EventBus") is null)
{
    var hostName = eventBusCfg["HostName"] ?? "localhost";
    var userName = eventBusCfg["UserName"] ?? "guest";
    var password = eventBusCfg["Password"] ?? "guest";
    var port = eventBusCfg["Port"] is { } p && int.TryParse(p, out var parsedPort) ? parsedPort : 5672;
    builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(_ => new RabbitMQ.Client.ConnectionFactory
    {
        HostName = hostName,
        UserName = userName,
        Password = password,
        Port = port
    });
}
else
{
    builder.AddRabbitMQClient("EventBus");
}
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

// EF Core 结构迁移自动应用（幂等：仅执行未应用的迁移；与 Identity/Markdown/Message 启动逻辑对齐）。
// 迁移失败仅告警、不阻断服务启动。
try
{
    using var efScope = app.Services.CreateScope();
    var efDbContext = efScope.ServiceProvider.GetRequiredService<VideoDbContext>();
    await efDbContext.Database.MigrateAsync();
    Console.WriteLine("[Video] EF Core 迁移已应用");
}
catch (Exception ex)
{
    Console.WriteLine($"[Video] EF Core 迁移应用失败（不影响启动，可在部署后手动 dotnet ef database update）: {ex.Message}");
}

app.MapDefaultEndpoints();

// S-16：全局异常脱敏（无内部路径/堆栈泄漏），必须位于管道最前
app.UseNotBlogExceptionHandler();

// 统一 API 响应包装：/api 下 JSON 响应自动包装为 {statusCode,message,responseData,isSuccess,responseDateTime} 信封
app.UseApiResponseWrapping();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// HTTPS 由网关/边缘终止，服务内不重定向：否则内部 http 请求会被 307 到服务 HTTPS 端口，导致请求绕过网关直连
// app.UseHttpsRedirection();

// JWT 认证中间件（S-07）
app.UseAuthentication();
app.UseAuthorization();

// 权限强制（服务内本地判定，读 JWT permissions claim）
app.UsePermissionEnforcement();

// --- MiniAPI Endpoint Registration ---
app.MapAddVideoEndpoints();
app.MapVideoEndpoints();
app.MapVideoAuditEndpoints();
app.MapVideoCollectionEndpoints();
app.MapVideoReviewEndpoints();
app.MapVideoBarrageEndpoints();
app.MapVideoStreamEndpoints();
app.MapVideoWatchStatsEndpoints();

app.Run();
