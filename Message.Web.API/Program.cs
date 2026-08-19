using System.Reflection;
using CacheMemory.Extensions;
using Message.Infrastructure;
using NotBlog.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");

builder.AddRabbitMQClient("EventBus");
builder.Services.AddEventBus(builder.Configuration.GetConnectionString("EventBus")??
    throw new ArgumentNullException("The Message for RabbitMQ connectionString is null!"),
    Assembly.GetExecutingAssembly());
// ⚠️ 2026-08-13 修复：AddNpgsql 来自纯 EF Npgsql 包（非 Aspire），参数是连接串字面量而非连接名——
// 旧写法 "PostgresSQL" 被当作连接串解析（运行时 index 0 报错），从未真正连上数据库

if (builder.Configuration.GetConnectionString("MessagePostgres") is null)
{
    Console.WriteLine("单个服务执行！");
    builder.Configuration["ConnectionStrings:MessagePostgres"] =
        builder.Configuration.GetSection("DbContextOption").GetValue<string>("DbContextConnection")
        ?? throw new ArgumentNullException("数据库连接字符未配置");
}
else
{
    Console.WriteLine("Aspire服务执行！");
}

builder.AddNpgsqlDbContext<MessageDbContext>("MessagePostgres");

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddMessageInfrastructure(builder.Configuration);
builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MessageDbContext>());
builder.Services.AddHttpContextAccessor();

// ═══ EventBus 消费者注册（RabbitMQ）：扫描 Web.API 程序集的集成事件处理器（如 RegisterByUserIntegrationEvent）═══
// 注：RabbitMqEventBus 为 Singleton 非 Try 注册，只能调用一次（Infrastructure 内不再重复注册）
builder.Services.AddEventBus(builder.Configuration.GetSection("EventBus"), Assembly.GetExecutingAssembly());

// ═══ Web 应用层服务统一注册（SignalR / JWT 认证 / gRPC 文件客户端 / 推送服务 / CORS） ═══
builder.Services.AddMessageWebApiServices(builder.Configuration, builder.Environment);

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

app.MapHub<MessageHub>("/MessageHub");
app.MapHub<CommunityHub>("/CommunityHub");
app.MapHub<CallHub>("/CallHub");

app.Run();