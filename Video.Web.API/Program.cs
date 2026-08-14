using System.Reflection;
using CacheMemory.Extensions;
using Commons.Web;
using NotBlog.ServiceDefaults;
using Notcomd.EventBus.Extension;
using Notcomd.Token.JWT.Extensions;
using NotMediator;
using Scalar.AspNetCore;
using Video.Domain.IServices;
using Video.Infrastructure;
using Video.Infrastructure.EntityFramework;
using Video.Web.API.Apis;
using Video.Web.API.Application.Commands;
using Video.Web.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ⚠️ 2026-08-13 修复：AddNpgsql 来自纯 EF Npgsql 包（非 Aspire），参数是连接串字面量而非连接名
builder.Services.AddNpgsql<VideoDbContext>(
    builder.Configuration.GetConnectionString("VideoPostgres")
    ?? throw new InvalidOperationException(
        "未配置数据库连接字符串：请设置环境变量 ConnectionStrings__VideoPostgres。"));

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
