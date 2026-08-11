using System.Reflection;
using Message.Infrastructure;
using NotBlog.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache("Redis");

// ═══ EventBus（RabbitMQ）：社区事件总线 ═══
// DEBUG：手动 ConnectionFactory（appsettings EventBus 节）；Release：Aspire 服务发现 AddRabbitMQClient("EventBus")
#if DEBUG
builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(_ =>
{
    var eventBusCfg = builder.Configuration.GetSection("EventBus");
    return new RabbitMQ.Client.ConnectionFactory
    {
        HostName = eventBusCfg["HostName"] ?? "localhost",
        UserName = eventBusCfg["UserName"] ?? "guest",
        Password = eventBusCfg["Password"] ?? "guest"
    };
});
#else
builder.AddRabbitMQClient("EventBus");
#endif
builder.Services.AddNpgsql<MessageDbContext>("PostgresSQL");
builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddMessageInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MessageDbContext>());
builder.Services.AddHttpContextAccessor();

// ═══ EventBus 消费者注册（RabbitMQ）：扫描 Web.API 程序集的集成事件处理器（如 RegisterByUserIntegrationEvent）═══
// 注：RabbitMqEventBus 为 Singleton 非 Try 注册，只能调用一次（Infrastructure 内不再重复注册）
builder.Services.AddEventBus(builder.Configuration.GetSection("EventBus"), Assembly.GetExecutingAssembly());

// ═══ Web 应用层服务统一注册（SignalR / JWT 认证 / gRPC 文件客户端 / 推送服务 / CORS） ═══
builder.Services.AddMessageWebApiServices(builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseHttpsRedirection();

app.UseExceptionHandling();
app.UseAuthentication();
app.UseUserContext();
app.UseAuthorization();

app.MapAuditApi();
app.MapCommentsApi();
app.MapFilesApi();
app.MapFriendsApi();
app.MapGroupsApi();
app.MapMessagesApi();
app.MapReportsApi();
app.MapSessionsApi();
app.MapTweetsApi();
app.MapCirclesApi();
app.MapTopicsApi();
app.MapFollowsApi();
app.MapNotificationsApi();
app.MapUserInfoApi();

app.MapHub<Message.Web.API.Hubs.MessageHub>("/MessageHub");
app.MapHub<Message.Web.API.Hubs.CommunityHub>("/CommunityHub");

app.Run();