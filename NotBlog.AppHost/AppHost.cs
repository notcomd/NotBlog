using Aspire.Hosting.ApplicationModel;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
// 基础设施资源（本机服务连接串）
// 说明（本地模式）：当前网络环境无法从 Docker Hub/国内镜像源拉取大镜像，
// 故改为连接本机已运行的 PostgreSQL(127.0.0.1:5432, postgres 免密) /
// Redis(127.0.0.1:6379) / RabbitMQ(127.0.0.1:5672, guest/guest)。
// 数据库（identity/notfile/message/video/markdownpostgres）已在本地建好；
// 如需恢复容器模式，将下方替换回 AddPostgres/AddRedis/AddRabbitMQ 即可。
// ═══════════════════════════════════════════════════════════════════

// 注意：Aspire 13.4 中 AddConnectionString(name, string) 已不存在（该签名现被解析为
// AddConnectionString(name, environmentVariableName)，会把字符串当作环境变量名），
// 必须使用 AddConnectionString(name, ReferenceExpression.Create($"...")) 显式传值。
// 本地 PostgreSQL 为 trust 免密认证，Password 为占位值，用于满足各服务 S-01 凭据外置校验。

// PostgreSQL：本机实例，按模块拆库；数据库名即各服务期望的连接串名称（F-03 命名约定保持不变）。
var identityDb = builder.AddConnectionString("IdentityPostgres", ReferenceExpression.Create($"Host=127.0.0.1;Port=5432;Database=identitypostgres;Username=postgres;Password=postgres"));  // Identity.Web.API（RELEASE 读 ConnectionStrings:IdentityPostgres）
var notfileDb = builder.AddConnectionString("NotFilePostgres", ReferenceExpression.Create($"Host=127.0.0.1;Port=5432;Database=notfilepostgres;Username=postgres;Password=postgres"));    // FileDev.Web.API（经环境变量 DbContextConnect 注入）
var messageDb = builder.AddConnectionString("MessagePostgres", ReferenceExpression.Create($"Host=127.0.0.1;Port=5432;Database=messagepostgres;Username=postgres;Password=postgres"));    // Message.Web.API（经 connectionName 映射为 "PostgresSQL"）
var videoDb = builder.AddConnectionString("VideoPostgres", ReferenceExpression.Create($"Host=127.0.0.1;Port=5432;Database=videopostgres;Username=postgres;Password=postgres"));          // Video.Web.API（AddNpgsql("VideoPostgres")）
var markDb = builder.AddConnectionString("MarkDownPostgres", ReferenceExpression.Create($"Host=127.0.0.1;Port=5432;Database=markdownpostgres;Username=postgres;Password=postgres"));     // Markdown.Web.API（AddNpgsql("MarkDownPostgres")）

// Redis：本机实例。不同模块期望的连接名不同（Identity/Message 用 "Redis"，Video/FileDev 用 "CacheMemory"），
// 通过 WithReference(connectionName:) 将同一实例按各自期望的名称注入。
var redis = builder.AddConnectionString("Redis", ReferenceExpression.Create($"127.0.0.1:6379"));

// RabbitMQ：本机实例（guest/guest），命名 EventBus，与各服务 AddRabbitMQClient("EventBus") 对齐。
var rabbitmq = builder.AddConnectionString("EventBus", ReferenceExpression.Create($"amqp://guest:guest@127.0.0.1:5672"));

// ═══════════════════════════════════════════════════════════════════
// 业务服务（项目）
// ═══════════════════════════════════════════════════════════════════

// FileDev：文件服务。
// - 数据库：FileDev 从配置键 DbContextConnect 读取连接串（而非 ConnectionStrings），故经环境变量覆盖注入容器连接串；
// - 缓存：AddCacheMemory(builder.Configuration) 读取 ConnectionStrings:CacheMemory；
// - RabbitMQ：手动从 EventBus 配置节创建连接（appsettings 默认 127.0.0.1:5672 guest/guest，与容器默认一致）。
var filedev = builder.AddProject<FileDev_Web_API>("filedev-web-api")
    .WithReference(notfileDb)
    .WithReference(redis, connectionName: "CacheMemory")
    .WithReference(rabbitmq)
    .WithEnvironment("DbContextConnect", notfileDb);

// Identity：账号服务。
// - 数据库：RELEASE 读 ConnectionStrings:IdentityPostgres；DEBUG 读 DbContextOption:DbContextConnect（同样注入容器连接串）；
// - 缓存：AddCacheMemory("Redis")；RabbitMQ：AddRabbitMQClient("EventBus")；
// - gRPC 调用 FileDev：FileStorageGrpc:Address 改用服务发现名，配合 WithReference(filedev) 解析真实端点。
var identity = builder.AddProject<Identity_Web_API>("identity-web-api")
    .WithReference(identityDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(filedev)
    .WithEnvironment("FileStorageGrpc__Address", "https://filedev-web-api")
    .WithEnvironment("DbContextOption__DbContextConnect", identityDb);

// Message：消息服务。数据库期望连接名 "PostgresSQL"（非 MessagePostgres），缓存用 "Redis"；
// 经服务发现（WithReference(filedev)）调用 FileDev 的文件上传 gRPC 服务。
var message = builder.AddProject<Message_Web_API>("message-web-api")
    .WithReference(messageDb, connectionName: "PostgresSQL")
    .WithReference(redis)
    .WithReference(filedev);

// Markdown：数据库 AddNpgsql("MarkDownPostgres")；RabbitMQ AddRabbitMQClient("EventBus")（RELEASE）。
var markdown = builder.AddProject<Markdown_Web_API>("markdown-web-api")
    .WithReference(markDb)
    .WithReference(rabbitmq);

// Video：数据库 AddNpgsql("VideoPostgres")；缓存 AddCacheMemory("CacheMemory")；
// RabbitMQ AddRabbitMQClient("EventBus")（RELEASE）；FileDev:BaseUrl 改用服务发现名（FileDevProxy HttpClient 已启用服务发现）。
var video = builder.AddProject<Video_Web_API>("video-web-api")
    .WithReference(videoDb)
    .WithReference(redis, connectionName: "CacheMemory")
    .WithReference(rabbitmq)
    .WithEnvironment("FileDev__BaseUrl", "http://filedev-web-api");

// YARP 网关：接入服务发现（WithReference 注入各服务的 services__<name>__http/https 端点），
// appsettings.json 中集群地址与 IdentityService:BaseUrl 改用虚拟主机名（https://<service-name>）。
builder.AddProject<NotBlog_Yarp>("notblog-yarp-gateway")
    .WithReference(identity)
    .WithReference(message)
    .WithReference(markdown)
    .WithReference(video)
    .WithReference(filedev);

builder.Build().Run();
