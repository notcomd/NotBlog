using System.Reflection;
using Commons.Web;
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

// 凭据外置（S-01）：数据库口令从环境变量 MARKDOWN_DB_PASSWORD 读取并补全连接串
var markdownConn = builder.Configuration.GetConnectionString("MarkDownPostgres");
if (markdownConn is not null && !markdownConn.Contains("Password=", StringComparison.OrdinalIgnoreCase))
{
    var markdownPassword = Environment.GetEnvironmentVariable("MARKDOWN_DB_PASSWORD");
    if (string.IsNullOrWhiteSpace(markdownPassword))
        throw new InvalidOperationException(
            "数据库连接串 MarkDownPostgres 未包含 Password，且环境变量 MARKDOWN_DB_PASSWORD 未设置，无法启动。");
    builder.Configuration["ConnectionStrings:MarkDownPostgres"] =
        markdownConn.TrimEnd(';') + $";Password={markdownPassword};";
}

// 配置 PostgreSQL DbContext
builder.Services.AddNpgsql<MarkDownDbContext>(builder.Configuration.GetConnectionString("MarkDownPostgres")!);

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
builder.Services.AddScoped<Markdown.Domain.IServices.ICurrentUserService, Markdown.Web.API.Services.CurrentUserService>();

// ClientRequest 幂等记录过期清理（每日执行，保留 7 天）
builder.Services.AddHostedService<ClientRequestCleanupService>();

builder.Services.AddAuthorization();

var app = builder.Build();

ResourcesBanner.PrintStartupBanner();

app.MapDefaultEndpoints();

// 业务异常统一映射（必须位于脱敏中间件之前：先识别业务异常，未识别的交给下方脱敏兜底）
app.UseExceptionHandler();

// S-16：全局异常脱敏（无内部路径/堆栈泄漏），必须位于管道最前
app.UseNotBlogExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();


app.MapMarkdownApis();

app.MapMarkFavoriteApi();

app.Run();
