using System.Reflection;
using CacheMemory.Extensions;
using Commons.Web;
using FileDev.Web.API.Grpc;
using Markdown.Infrastructure;
using Markdown.Web.API.Apis;
using Markdown.Web.API.Extensions;
using Markdown.Web.API.Resources;
using Markdown.Web.API.Services;
using NotBlog.ServiceDefaults;
using Notcomd.Token.JWT.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 配置 JWT Bearer 认证（S-10：与 Identity 签发配置对齐，开启 ValidateAudience/ValidateIssuerSigningKey）
// 使用 JWToken 库标准入口 AddJwtAuthentication(IConfiguration)，Issuer/Audience/密钥全站一致，
// 密钥凭据外置（JwtOptions:PrivateKey 或环境变量 JWT_PRIVATE_KEY，见 JWToken/AuthenticationExtensions）
builder.Services.AddJwtAuthentication(builder.Configuration.GetSection("JwtOptions"));


if (builder.Configuration.GetConnectionString("MarkDownPostgres") is null)
{
    Console.WriteLine("单个服务执行！");
    builder.Configuration["ConnectionStrings:MarkDownPostgres"] =
        builder.Configuration.GetSection("DbContextOption").GetValue<string>("DbContextConnection")
        ?? throw new ArgumentNullException("数据库连接字符未配置");
}
else
{
    Console.WriteLine("Aspire服务执行！");
}

builder.AddNpgsqlDbContext<MarkDownDbContext>("MarkDownPostgres");


// 配置 Markdown 基础设施（仓储等）
builder.Services.AddMarkdownInfrastructure();

// 配置 NotMediator（领域事件中介）
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

// 启动时自动执行数据库迁移（markdown_db 建表）
builder.Services.AddMigration<MarkDownDbContext>();


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

// OpenAPI/Swagger 配置
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();

// 统一业务异常映射（KeyNotFoundException→404 / 越权→403 / 非法参数→400），
// 未识别的异常返回 false 交由 ExceptionSanitizingMiddleware 脱敏为 500
builder.Services.AddExceptionHandler<MarkdownApiExceptionHandler>();



// 当前用户服务
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService,CurrentUserService>();

// Markdown 正文文件存储：默认 FileDev gRPC（AppHost 环境）；
// 配置 MarkdownContent:Provider=Local 时回退本地磁盘（单机调试，appsettings.Development.json）
builder.Services.Configure<FileStorageGrpcOptions>(
    builder.Configuration.GetSection(FileStorageGrpcOptions.SectionName));

builder.Services.AddGrpcClient<FileStorage.FileStorageClient>(
    FileDevMarkdownContentStore.ClientName,
    options =>
    {
        // 优先 Aspire 服务发现解析 FileDev 地址（服务名 filedev-web-api）；
        // 脱离 AppHost 独立启动时使用 appsettings FileStorageGrpc:Address 兜底
        var configuredAddress = builder.Configuration["FileStorageGrpc:Address"];
        options.Address = new Uri(string.IsNullOrWhiteSpace(configuredAddress)
            ? $"https://{FileDevMarkdownContentStore.ClientName}"
            : configuredAddress);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
#if DEBUG
        // S-16：仅 DEBUG/开发环境允许跳过证书校验；Release 下使用系统默认证书校验
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
#endif
    });

if (builder.Configuration["MarkdownContent:Provider"] == "Local")
{
    // 本地磁盘实现只依赖单例服务（IWebHostEnvironment/ILogger），保持单例
    builder.Services.AddSingleton<IMarkdownContentStore, LocalMarkdownContentStore>();
}
else
{
    // 必须 Scoped：FileDev 实现依赖 Scoped 的 IJwtTokenService 与 IOptionsSnapshot<JwtOptions>，
    // 注册为 Singleton 会在容器校验阶段抛 "Cannot consume scoped service ... from singleton" 导致启动失败。
    builder.Services.AddScoped<IMarkdownContentStore, FileDevMarkdownContentStore>();

    // 热点榜 Redis 缓存（Aspire 环境注入 ConnectionStrings:Redis；Local 模式不注册，热点服务自动降级 DB）
    builder.AddCacheMemory("Redis");
}

// 热点榜服务 + 定时重建（Redis 缺失时自动降级 DB 实时计算）
builder.Services.AddScoped<IMarkdownHotBoardService, MarkdownHotBoardService>();
builder.Services.AddHostedService<MarkdownHeatRebuildBackgroundService>();

// ClientRequest 幂等记录过期清理（每日执行，保留 7 天）
builder.Services.AddHostedService<ClientRequestCleanupService>();

builder.Services.AddAuthorization();

var app = builder.Build();

ResourcesBanner.PrintStartupBanner();

app.MapDefaultEndpoints();

// S-16：全局异常脱敏（无内部路径/堆栈泄漏），必须位于管道最前
app.UseNotBlogExceptionHandler();

// 统一响应包装（ApiResponseResult 信封）：位于脱敏之后、认证之前，
// 包装所有 /api JSON 端点响应；未处理异常已由脱敏中间件直接输出统一信封
app.UseApiResponseWrapping();

// 业务异常统一映射：注册在包装中间件内层，其写出的统一信封由上方包装中间件透传，未识别的异常继续上抛脱敏兜底
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// HTTPS 由网关/边缘终止，服务内不重定向：否则内部 http 请求会被 307 到服务 HTTPS 端口，导致请求绕过网关直连
// app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();
app.UsePermissionEnforcement();


app.MapMarkdownApis();

app.MapMarkFavoriteApi();

app.Run();
