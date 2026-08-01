using System.Reflection;
using CacheMemory.Extensions;
using NotBlog.ServiceDefaults;
using Notcomd.EventBus.Extension;
using NotMediator;
using Scalar.AspNetCore;
using Video.Infrastructure;
using Video.Infrastructure.EntityFramework;
using Video.Web.API.Apis;
using Video.Web.API.Application.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddNpgsql<VideoDbContext>("VideoPostgres");

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

builder.Services.AddAuthorization();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// --- MiniAPI Endpoint Registration ---
app.MapAddVideoEndpoints();
app.MapVideoEndpoints();
app.MapVideoCollectionEndpoints();
app.MapVideoReviewEndpoints();
app.MapVideoBarrageEndpoints();
app.MapVideoStreamEndpoints();
app.MapVideoWatchStatsEndpoints();

app.Run();
