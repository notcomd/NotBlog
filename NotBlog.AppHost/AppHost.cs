using Aspire.Hosting.ApplicationModel;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
// 基础设施资源（Aspire 托管容器：PostgreSQL / Redis / RabbitMQ）
// 说明（2026-08-17 容器模式）：由 Aspire 拉起容器资源（Docker Desktop / Podman），
// 各服务经 WithReference 注入连接串（ConnectionStrings__<name>），注入名与各服务
// GetConnectionString(...) 的 key 精确对齐；RabbitMQ 固定宿主端口 5672（Message/FileDev
// 的 DEBUG 手动配置节仍指向 localhost/127.0.0.1:5672 guest/guest，零改动兼容——Aspire
// AddRabbitMQ 默认凭据即 guest/guest）。
// ⚠️ 镜像拉取：默认 docker.io 官方镜像；如网络受限，用 .WithImage(...) 换国内镜像源
// （如 docker.m.daocloud.io/library/postgres:17.4）。
// ⚠️ 数据：容器是全新空库（非本机 PG 的旧数据）——Identity 启动时 Migrate + 权限数据需重新灌入、
// Message 需执行 Infrastructure/Sql/MessageSchema.sql（其余服务有 EF Migrations 自动建表）；
// .WithDataVolume() 保证容器重启后数据不丢。
// ═══════════════════════════════════════════════════════════════════

// PostgreSQL：单实例多库。AddDatabase(注入连接名, PG库名)——连接名保持各服务期望值。
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var identityDb = postgres.AddDatabase("IdentityPostgres", "identitypostgres");      // Identity.Web.API（GetConnectionString("IdentityPostgres")）
var notfileDb = postgres.AddDatabase("NotFilePostgres", "notfilepostgres");         // FileDev.Web.API（GetconnectionString("FileDevPostgres")）
var messageDb = postgres.AddDatabase("MessagePostgres", "messagepostgres");         // Message.Web.API（GetConnectionString("MessagePostgres")）
var videoDb = postgres.AddDatabase("VideoPostgres", "videopostgres");               // Video.Web.API（GetConnectionString("VideoPostgres")）
var markDb = postgres.AddDatabase("MarkDownPostgres", "markdownpostgres");          // Markdown.Web.API（GetConnectionString("MarkDownPostgres")）

// Redis：单实例。Identity/Message 读连接名 "Redis"，Video/FileDev 经 WithReference(connectionName:"CacheMemory") 注入。
var redis = builder.AddRedis("Redis")
    .WithDataVolume();

// RabbitMQ：单实例（guest/guest，与 Message/FileDev DEBUG 手动配置节默认值一致）。
// 固定宿主端口 5672：手动配置节（localhost:5672 / 127.0.0.1:5672）无需改动即可连通；
// ⚠️ 要求本机 5672 空闲（停掉本机 RabbitMQ 实例，否则端口绑定冲突）。
var rabbitmq = builder.AddRabbitMQ("EventBus")
    .WithEndpoint(port: 5672, targetPort: 5672, name: "tcp")
    .WithDataVolume();



// ═══════════════════════════════════════════════════════════════════
// 业务服务（项目）
// ═══════════════════════════════════════════════════════════════════

// 网关内部调用凭证（V4）：从 AppHost 配置（GatewayInternal:ApiKey / 环境变量
// GatewayInternal__ApiKey）透传给 Identity 与网关，二者必须一致；
// 未配置时网关启动会因凭证缺失而失败（fail-closed，不静默降级）。
var gatewayInternalApiKey = builder.Configuration["GatewayInternal:ApiKey"];

// FileDev：文件服务。
// - 数据库：FileDev 从配置键 DbContextConnect 读取连接串（而非 ConnectionStrings），故经环境变量覆盖注入容器连接串；
// - 缓存：AddCacheMemory(builder.Configuration) 读取 ConnectionStrings:CacheMemory；
// - RabbitMQ：手动从 EventBus 配置节创建连接（appsettings 默认 127.0.0.1:5672 guest/guest，与容器默认一致）。
var filedev = builder.AddProject<FileDev_Web_API>("filedev-web-api")
    .WithReference(notfileDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(postgres);

// Identity：账号服务。
// - 数据库：RELEASE 读 ConnectionStrings:IdentityPostgres；DEBUG 读 DbContextOption:DbContextConnection（同样注入容器连接串）；
// - 缓存：AddCacheMemory("Redis")；RabbitMQ：AddRabbitMQClient("EventBus")；
// - gRPC 调用 FileDev：FileStorageGrpc:Address 改用服务发现名，配合 WithReference(filedev) 解析真实端点。
var identity = builder.AddProject<Identity_Web_API>("identity-web-api")
    .WithReference(identityDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(filedev)
    .WaitFor(postgres);

// V4：网关内部调用凭证（未配置时不注入，Identity 侧保持 fail-closed）
if (!string.IsNullOrEmpty(gatewayInternalApiKey))
    identity.WithEnvironment("GATEWAY_INTERNAL_API_KEY", gatewayInternalApiKey);

// Message：消息服务。数据库期望连接名 "PostgresSQL"（非 MessagePostgres），缓存用 "Redis"；
// 经服务发现（WithReference(filedev)）调用 FileDev 的文件上传 gRPC 服务。
var message = builder.AddProject<Message_Web_API>("message-web-api")
    .WithReference(messageDb)
    .WithReference(redis)
    .WithReference(filedev)
    .WithReference(rabbitmq)
    .WaitFor(postgres); // 等待数据库和 RabbitMQ 容器就绪（RELEASE）

// Markdown：数据库 AddNpgsql("MarkDownPostgres")；RabbitMQ AddRabbitMQClient("EventBus")（RELEASE）。
var markdown = builder.AddProject<Markdown_Web_API>("markdown-web-api")
    .WithReference(markDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WaitFor(postgres); // 等待数据库、RabbitMQ 和 Redis 容器就绪（RELEASE）

// Video：数据库 AddNpgsql("VideoPostgres")；缓存 AddCacheMemory("CacheMemory")；
// RabbitMQ AddRabbitMQClient("EventBus")（RELEASE）；FileDev:BaseUrl 改用服务发现名（FileDevProxy HttpClient 已启用服务发现）。
var video = builder.AddProject<Video_Web_API>("video-web-api")
    .WithReference(videoDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(filedev)
    .WaitFor(postgres);

// YARP 网关：接入服务发现（WithReference 注入各服务的 services__<name>__http/https 端点），
// appsettings.json 中集群地址与 IdentityService:BaseUrl 改用虚拟主机名（https://<service-name>）。
var gateway = builder.AddProject<NotBlog_Yarp>("notblog-yarp-gateway")
    .WithReference(identity)
    .WithReference(message)
    .WithReference(markdown)
    .WithReference(video)
    .WithReference(filedev);

// V4：网关内部调用凭证（与 Identity 侧保持一致；未配置时网关启动将失败）
if (!string.IsNullOrEmpty(gatewayInternalApiKey))
    gateway.WithEnvironment("GATEWAY_INTERNAL_API_KEY", gatewayInternalApiKey);

// ═══════════════════════════════════════════════════════════════════
// Docker Compose 部署目标（2026-08-17 添加）
// aspire add docker 已引入 Aspire.Hosting.Docker 13.4.6；此环境资源使
// aspire publish / aspire deploy 可生成 docker-compose.yaml + .env 部署工件。
// 单 compute environment（compose 即唯一部署目标），无需 WithComputeEnvironment。
// 生成物：aspire publish -o <dir> → docker-compose.yaml / .env / 各服务 Dockerfile。
// ═══════════════════════════════════════════════════════════════════
builder.AddDockerComposeEnvironment("docker-compose");

builder.Build().Run();
