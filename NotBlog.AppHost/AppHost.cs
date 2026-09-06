using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

using Projects;

var builder = DistributedApplication.CreateBuilder(args);


// 宿主端口映射（供本机数据库客户端直连，同时避开本机常驻服务）：
//   5433 -> 5432  PostgreSQL；6380 -> 6379 Redis；5673 -> 5672 RabbitMQ（本机 5672 常被占用）
var postgres = builder.AddPostgres("postgres")
    //.WithImagePullPolicy(ImagePullPolicy.Never)
    .WithDataVolume()
    .WithHostPort(5433);

var identityDb = postgres.AddDatabase("IdentityPostgres", "identitypostgres");      // Identity.Web.API（GetConnectionString("IdentityPostgres")）
var notfileDb = postgres.AddDatabase("NotFilePostgres", "notfilepostgres");         // FileDev.Web.API（GetconnectionString("FileDevPostgres")）
var messageDb = postgres.AddDatabase("MessagePostgres", "messagepostgres");         // Message.Web.API（GetConnectionString("MessagePostgres")）
var videoDb = postgres.AddDatabase("VideoPostgres", "videopostgres");               // Video.Web.API（GetConnectionString("VideoPostgres")）
var markDb = postgres.AddDatabase("MarkDownPostgres", "markdownpostgres");          // Markdown.Web.API（GetConnectionString("MarkDownPostgres")）

// Redis：单实例。Identity/Message 读连接名 "Redis"，Video/FileDev 经 WithReference(connectionName:"CacheMemory") 注入。
var redis = builder.AddRedis("Redis")
    //.WithImagePullPolicy(ImagePullPolicy.Never)
    .WithDataVolume()
    .WithHostPort(6380);


// MongoDB：服务于 FileDev 的分片上传跟踪 / Message 的对话迁移数据（连接串经 WithEnvironment 显式注入）。
// 本环境改用 AddContainer 管理（AddMongoDB 集成反复 FailedToStart，而同链路的
// postgres/redis/rabbitmq 容器均正常）；mongo:8 已本地预拉（ImagePullPolicy.Never 不触网）。
// 宿主固定 27018→容器 27017，避开本机常驻 mongod 的 27017。
var mongo = builder.AddContainer("NotFileMongo", "mongo:8")
    //.WithImagePullPolicy(ImagePullPolicy.Never)
    .WithVolume("notblog-mongo-data", "/data/db")
    .WithEndpoint(port: 27018, targetPort: 27017, name: "mongo-port");


var rabbitmq = builder.AddRabbitMQ("EventBus")
    //.WithImagePullPolicy(ImagePullPolicy.Never)
    .WithDataVolume()
    .WithEndpoint(port: 5673, targetPort: 5672, name: "tcp");





var filedev = builder.AddProject<FileDev_Web_API>("filedev-web-api")
    .WithReference(notfileDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__NotFileMongo", "mongodb://localhost:27018")
    .WaitFor(postgres);
 


var identity = builder.AddProject<Identity_Web_API>("identity-web-api")
    .WithReference(identityDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(filedev)
    .WaitFor(postgres);




var message = builder.AddProject<Message_Web_API>("message-web-api")
    .WithReference(messageDb)
    .WithReference(redis)
    .WithReference(filedev)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__MessageMongo", "mongodb://localhost:27018")
    .WaitFor(postgres); 


var turnSection = builder.Configuration.GetSection("TurnService");
if (turnSection.Exists())
{
    var turnSecret = turnSection["SharedSecret"];
    if (!string.IsNullOrWhiteSpace(turnSecret))
        message.WithEnvironment("TurnService__SharedSecret", turnSecret);

    var ttl = turnSection["TtlSeconds"];
    if (!string.IsNullOrWhiteSpace(ttl))
        message.WithEnvironment("TurnService__TtlSeconds", ttl);

    var turnUrls = turnSection.GetSection("Urls").Get<string[]>();
    if (turnUrls is { Length: > 0 })
    {
        for (var i = 0; i < turnUrls.Length; i++)
            message.WithEnvironment($"TurnService__Urls__{i}", turnUrls[i]);
    }
}


var markdown = builder.AddProject<Markdown_Web_API>("markdown-web-api")
    .WithReference(markDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReference(filedev)
    .WaitFor(postgres);


var video = builder.AddProject<Video_Web_API>("video-web-api")
    .WithReference(videoDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(filedev)
    .WaitFor(postgres);


var gateway = builder.AddProject<NotBlog_Yarp>("notblog-yarp-gateway")
    // 2026-09-06 权限验证下沉各服务：网关透明转发，不再需要 Redis/RabbitMQ/内部调用凭证
    .WithReference(identity)
    .WithReference(message)
    .WithReference(markdown)
    .WithReference(video)
    .WithReference(filedev);


// var frontend = builder.AddProject<Not>("notblog-frontend")
//     .WithEnvironment("GATEWAY_UPSTREAM", "host.docker.internal:5000")
//     .WithHttpEndpoint(port: 8080, targetPort: 80);


builder.AddDockerComposeEnvironment("docker-compose");

builder.Build().Run();
