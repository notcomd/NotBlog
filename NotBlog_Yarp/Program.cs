using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NotBlog_Yarp.Middlewares;
using NotBlog_Yarp.Permission;
using NotBlog_Yarp.Transforms;
using System.Text;
using NotBlog.ServiceDefaults;
using Yarp.ReverseProxy.Transforms.Builder;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ========== 1. JWT 认证配置 ==========
var jwtSection = builder.Configuration.GetSection("JwtSettings");
// 凭据外置（S-01）：签名密钥从配置或共享环境变量 JWT_PRIVATE_KEY 读取，缺失时报清晰错误。
var secretKey = jwtSection["SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_PRIVATE_KEY");
if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException(
        "JWT 签名密钥未配置：请在环境变量 JWT_PRIVATE_KEY（或配置 JwtSettings:SecretKey）中设置。");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

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

if (!string.IsNullOrWhiteSpace(identityBaseUrl))
{
    // 生产模式：HTTP 调用 Identity 服务
    // 弹性（F-12）：AddServiceDefaults 已通过 ConfigureHttpClientDefaults 为所有 HttpClient
    // 注册 AddStandardResilienceHandler（重试/熔断/attempt+total 超时），此处不重复注册；
    // 放开总超时，避免短 Timeout 掐断标准弹性重试（attempt 超时由处理器内部兜底）。
    builder.Services.AddHttpClient<IPermissionServiceClient, HttpPermissionServiceClient>(client =>
    {
        client.BaseAddress = new Uri(identityBaseUrl);
        client.Timeout = Timeout.InfiniteTimeSpan;
    });

    // 注册映射加载器：启动后从 Identity 拉取权威映射，替换本地配置
    builder.Services.AddHostedService<PermissionMappingInitializer>();
}
else
{
    // 开发/测试模式：从配置文件读取用户权限（不依赖 Identity，支持 IOptionsMonitor 热重载）
    builder.Services.AddSingleton<IPermissionServiceClient, ConfigPermissionServiceClient>();
}

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
