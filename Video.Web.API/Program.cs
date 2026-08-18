
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
// IConnectionFactory 来源：Aspire AddRabbitMQClient("EventBus") 或手动注册
var eventBusCfg = builder.Configuration.GetSection("EventBus");
#if DEBUG
builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(_ =>
{
    var host = eventBusCfg["HostName"] ?? "localhost";
    var userName = eventBusCfg["UserName"] ?? "guest";
    var password = eventBusCfg["Password"] ?? "guest";
    return new RabbitMQ.Client.ConnectionFactory
    {
        HostName = host,
        UserName = userName,
        Password = password
    };
});
#else
builder.AddRabbitMQClient("EventBus");
#endif
builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();

// S-16：全局异常脱敏（无内部路径/堆栈泄漏），必须位于管道最前
app.UseNotBlogExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// JWT 认证中间件（S-07）
app.UseAuthentication();
app.UseAuthorization();

// --- MiniAPI Endpoint Registration ---
app.MapAddVideoEndpoints();
app.MapVideoEndpoints();
app.MapVideoCollectionEndpoints();
app.MapVideoReviewEndpoints();
app.MapVideoBarrageEndpoints();
app.MapVideoStreamEndpoints();
app.MapVideoWatchStatsEndpoints();

app.Run();
