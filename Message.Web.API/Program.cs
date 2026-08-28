using System.Reflection;
using CacheMemory.Extensions;
using Message.Infrastructure;
using Message.Infrastructure.MongoMigration;
using MongoDB.Driver;
using NotBlog.ServiceDefaults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCacheMemory("Redis");

builder.AddRabbitMQClient("EventBus");
builder.Services.AddEventBus(builder.Configuration.GetConnectionString("EventBus")??
    throw new ArgumentNullException("The Message for RabbitMQ connectionString is null!"),
    Assembly.GetExecutingAssembly());


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

// ── MongoDB（D2-1 对话迁移：message + chat_session 集合）──
// 单机分支：从配置节点 MongoDb:ConnectionString 读取，未配置时回落本地默认地址；
// Aspire 分支：通过 AddMongoDBClient 注入连接串。二者统一为单例 IMongoDatabase。
if (builder.Configuration.GetConnectionString("MessageMongo") is null)
{
    var mongoConn = builder.Configuration["MongoDb:ConnectionString"] ?? "mongodb://127.0.0.1:27017";
    builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConn));
}
else
{
    builder.AddMongoDBClient("MessageMongo");
}
builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var databaseName = builder.Configuration["MongoDb:Database"] ?? "notblog_message";
    return client.GetDatabase(databaseName);
});

builder.Services.AddNotMediator(Assembly.GetExecutingAssembly());

builder.Services.AddMessageInfrastructure(builder.Configuration);
// D2-1：消息/会话读写链路切换到 Mongo（message + chat_session 集合）；社交域仍留 EF。
builder.Services.AddMessageMongoRepositories();
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

// ═══ 存量聊天数据迁移（PG → Mongo）═══
// 一次性任务：设置 ChatMigration__BackfillOnStartup=true 启动即执行；
// 幂等（upsert），完成后无需保留该配置。默认关闭以不影响常规启动。
if (app.Configuration.GetValue<bool>("ChatMigration:BackfillOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    var migrator = migrationScope.ServiceProvider.GetRequiredService<MessageMongoMigrationService>();
    var migrationResult = await migrator.ExecuteAsync();
    Console.WriteLine($"[ChatMigration] 存量迁移完成：会话={migrationResult.SessionsMigrated}，消息={migrationResult.MessagesMigrated}");
}

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
app.MapTurnApi();

app.MapHub<MessageHub>("/MessageHub");
app.MapHub<CommunityHub>("/CommunityHub");
app.MapHub<CallHub>("/CallHub");

app.Run();