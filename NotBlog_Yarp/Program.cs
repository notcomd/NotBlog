
using System.Reflection;
using CacheMemory.Core;
using CacheMemory.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("EventBus");

// ═══ EventBus 注册（订阅 Identity 权限变更事件，事件驱动刷新路由映射） ═══
// 配置节提供队列名/交换机参数（SubscriptionClientName=yarp_permission_events）；
// 必须传入当前程序集以扫描注册 PermissionUpdatedEventHandler（不传则事件订阅为空）；
// IConnectionFactory 由上方 AddRabbitMQClient 提供（Aspire WithReference(rabbitmq) 注入连接串）。
// ⚠️ 全项目只可注册一次：AddEventBusInternal 对 RabbitMqEventBus/IEventBus/IHostedService
// 均为非幂等 AddSingleton，重复调用会导致 HostedService 启动两次（重复消费）。
builder.Services.AddEventBus(
    builder.Configuration.GetSection("EventBus"), Assembly.GetExecutingAssembly());

// ═══ Redis（权限映射主存） ═══
// Aspire 模式：ConnectionStrings:Redis 已注入（容器 Redis），Aspire 重载以连接串覆盖 Default 实例；
// 独立运行：回落到本地 CacheMemory 配置节（localhost:6379）。
// ⚠️ 二者只可注册其一：CacheMemoryOption 为非幂等 AddSingleton，后注册者会胜出。
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("Redis")))
{
    builder.AddCacheMemory("Redis");
}
else
{
    builder.Services.AddCacheMemory(builder.Configuration.GetSection("CacheMemory"));
}

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


// ========== 4. 权限服务客户端（根据配置选择实现，二者只注册其一） ==========
// Identity 地址解析优先级：
//   1. IDENTITY_WEB_API_HTTP 环境变量 — Aspire WithReference(identity) 注入
//      （run 模式含实际端口；compose 模式为容器服务名地址），两种环境下均优先；
//   2. IdentityService:BaseUrl 配置 — 显式配置的 Identity 地址；
//   3. 均未配置 → 开发/测试模式，回落 ConfigPermissionServiceClient（本地 DevUsers）。
// ⚠️ IPermissionServiceClient 双注册时 DI 取后者，曾导致 HttpPermissionServiceClient
// （含 5xx/429 fail-open、4xx fail-closed 降级）与映射同步链路整体失效，勿再叠加注册。
var identityBaseUrl =
    Environment.GetEnvironmentVariable("IDENTITY_WEB_API_HTTP")
    ?? builder.Configuration["IdentityService:BaseUrl"];

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

if (string.IsNullOrWhiteSpace(identityBaseUrl))
{
    // 开发/测试模式：从配置文件读取用户权限（不依赖 Identity，支持 IOptionsMonitor 热重载）
    builder.Services.AddSingleton<IPermissionServiceClient, ConfigPermissionServiceClient>();
}
else
{
    // 生产模式：HTTP 调用 Identity 服务
    // 弹性（F-12）：AddServiceDefaults 已通过 ConfigureHttpClientDefaults 为所有 HttpClient
    // 注册 AddStandardResilienceHandler（重试/熔断/attempt+total 超时），此处不重复注册；
    // 放开总超时，避免短 Timeout 掐断标准弹性重试（attempt 超时由处理器内部兜底）。
    builder.Services.AddHttpClient<IPermissionServiceClient, HttpPermissionServiceClient>(client =>
    {
        client.BaseAddress = new Uri(identityBaseUrl);
        client.Timeout = Timeout.InfiniteTimeSpan;
        client.DefaultRequestHeaders.Add("X-Internal-Api-Key", internalApiKey);
    });
}

// 注册映射加载器：启动后从 Identity 拉取权威映射，替换本地配置
builder.Services.AddHostedService<PermissionMappingInitializer>();

// 注册后台轮询服务：定期静默拉取最新权限映射
builder.Services.AddHostedService<PermissionMappingRefresher>();

// 权限映射 JSON + Redis 双层持久化（容错：Redis 不可用时从本地 JSON 加载）
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
