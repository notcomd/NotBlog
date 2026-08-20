
using System.Reflection;
using CacheMemory.Core;
using CacheMemory.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();





builder.AddRabbitMQClient("EventBus");

///本地运行
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("EventBus")))
{
    builder.Services.AddEventBus(builder.Configuration.GetConnectionString("EventBus") ?? 
    throw new ArgumentNullException("the NotBlog_Yarp for RabbitMQ connectionsting is null! "),
     Assembly.GetEntryAssembly() ?? throw new ArgumentNullException("无法获取程序集"));
}

if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")))
{
    builder.AddCacheMemory("Redis");
}

builder.Services.AddCacheMemory(builder.Configuration.GetSection("CacheMemory"));

builder.Services.AddEventBus(builder.Configuration.GetSection("EventBus"));

builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));
builder.Services.AddAuthorization();

// ========== 2. 权限配置（支持 IOptions 校验和热重载） ==========
builder.Services
    .AddOptions<PermissionOptions>()
    .Bind(builder.Configuration.GetSection(PermissionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ========== 3. 权限路由映射表 ==========
// 通过 PermissionOptions 构建，支持配置校验
var options = builder.Configuration
    .GetSection(PermissionOptions.SectionName)
    .Get<PermissionOptions>();

if (options is null)
{
    throw new InvalidOperationException(
        $"缺少配置节 \"{PermissionOptions.SectionName}\"，无法启动网关。");
}

var routeMap = PermissionRouteMap.FromOptions(options);
builder.Services.AddSingleton(routeMap);


// ========== 4. 权限服务客户端（根据配置选择实现） ==========
var identityBaseUrl = builder.Configuration["IdentityService:BaseUrl"];

// 内部调用凭证（V4）：网关 → Identity 的内部端点（权限检查/映射拉取）必须携带共享密钥，
// 与 Identity 侧 InternalApiKeyFilter 配对；未配置则启动失败（fail-closed，不静默降级）。
var internalApiKey = builder.Configuration["GatewayInternal:ApiKey"]
    ?? Environment.GetEnvironmentVariable("GATEWAY_INTERNAL_API_KEY");
if (string.IsNullOrWhiteSpace(internalApiKey))
{
    throw new InvalidOperationException(
        "网关内部调用凭证未配置：请在环境变量 GATEWAY_INTERNAL_API_KEY（或配置 GatewayInternal:ApiKey）中设置，"
        + "且与 Identity 服务保持一致。");
}

// 生产模式：HTTP 调用 Identity 服务
// 弹性（F-12）：AddServiceDefaults 已通过 ConfigureHttpClientDefaults 为所有 HttpClient
// 注册 AddStandardResilienceHandler（重试/熔断/attempt+total 超时），此处不重复注册；
// 放开总超时，避免短 Timeout 掐断标准弹性重试（attempt 超时由处理器内部兜底）。
builder.Services.AddHttpClient<IPermissionServiceClient, HttpPermissionServiceClient>(client =>
{
    client.BaseAddress = new Uri(identityBaseUrl ?? throw new ArgumentNullException("未设置连接字符串"));
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
});

// 注册映射加载器：启动后从 Identity 拉取权威映射，替换本地配置
builder.Services.AddHostedService<PermissionMappingInitializer>();

// 注册后台轮询服务：定期静默拉取最新权限映射
builder.Services.AddHostedService<PermissionMappingRefresher>();

// 注册权限映射 JSON + Redis 双层持久化（容错：Redis 不可用时从本地 JSON 加载）
// Redis 为主存（跨实例共享），JSON 文件为本地回退
// builder.Services.AddCacheMemory(builder.Configuration);
// builder.Services.AddSingleton<PermissionMappingStore>();

// 注册 EventBus：订阅 Identity 发布的权限变更事件，事件驱动秒级刷新映射
// IConnectionFactory 手动注册（Yarp 不经 Aspire AddRabbitMQClient，自行配置连接）
// var eventBusCfg = builder.Configuration.GetSection("EventBus");
// var hostName = eventBusCfg["HostName"] ?? "localhost";
// var userName = eventBusCfg["UserName"] ?? "guest";
// var password = eventBusCfg["Password"] ?? "guest";
// var port = int.TryParse(eventBusCfg["Port"], out var p) ? p : 5672;

// builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(_ =>
//     new RabbitMQ.Client.ConnectionFactory
//     {
//         HostName = hostName,
//         UserName = userName,
//         Password = password,
//         Port = port
//     });

//builder.Services.AddEventBus(eventBusCfg, Assembly.GetExecutingAssembly());

// 开发/测试模式：从配置文件读取用户权限（不依赖 Identity，支持 IOptionsMonitor 热重载）
builder.Services.AddSingleton<IPermissionServiceClient, ConfigPermissionServiceClient>();

builder.Services.AddSingleton<PermissionMappingStore>();
// ========== 5. YARP 反向代理 + 自定义 Header 注入 Transform ==========
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ========== 6. 注册自定义 Transform Provider ==========
builder.Services.AddSingleton<ITransformProvider, UserContextTransformProvider>();

var app = builder.Build();



// ========== 7. 中间件管道 ==========
// 顺序: 认证 → 授权 → 权限过滤 → YARP 转发 + UserContext Transform

app.UseAuthentication();
app.UseAuthorization();

// 权限过滤中间件（在 YARP 转发之前拦截）
app.UseMiddleware<PermissionFilterMiddleware>();

app.MapReverseProxy();

app.Run();
