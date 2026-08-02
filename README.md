# NotBlog

小型博客后端项目，采用 DDD + CQRS + EventBus + Aspire 的多模块微服务架构（.NET 10）。

## 模块结构

| 项目 | 职责 |
|------|------|
| `Identity.Web.API` | 用户认证/授权、OAuth 绑定、令牌体系 |
| `Message.Web.API` | 即时通讯（会话/消息/群组/Tweet）与 SignalR Hub |
| `FileDev.Web.API` | 文件上传下载、分片断点续传（HTTP + gRPC） |
| `Video.Web.API` | 视频发布、播放、点赞评论收藏 |
| `Markdown.Web.API` | Markdown 文章、评论、历史版本 |
| `NotBlog_Yarp` | YARP 网关（认证、路由、服务发现） |
| `NotBlog.AppHost` | Aspire 编排（Redis/Postgres/RabbitMQ + 各服务） |
| `JWToken` / `CacheMemory` / `Eventbus` / `DomainInfrastructure` / `CommonsInitializer` / `NotEmail` | 公共基础设施 |

## 前置依赖

- PostgreSQL（各 DbContext 数据库）
- Redis（CacheMemory：在线状态/令牌/幂等）
- RabbitMQ（EventBus + Outbox）

## 配置与连接串约定

敏感配置（JWT 私钥、OAuth ClientSecret、SMTP 口令、数据库/Redis/RabbitMQ 口令）一律通过环境变量注入，仓库内不存放真实凭据。

- DbContext 连接串：优先 `ConnectionStrings:DbContext:{ContextName}`（如 `ConnectionStrings:DbContext:NotBlogDbContext`），回退 `DbContextConnect` / `DefaultDB:ConnStr`
- Redis：`ConnectionStrings:Redis`（Aspire）或 `CacheMemory:Instances:*`
- RabbitMQ：`ConnectionStrings:EventBus` 或 `RabbitMQ:*` 环境变量

## 迁移

各 Infrastructure 项目包含 EF Core 迁移（如 `Markdown.Infrastructure`、`FileDev.Infrastructure`），服务启动时执行 `Migrate()` 自动建库建表。

## 启动

```bash
# 方式一：Aspire 编排（推荐，自动拉起 Redis/Postgres/RabbitMQ 与全部服务）
dotnet run --project NotBlog.AppHost

# 方式二：单独启动某服务（需先准备上述中间件）
dotnet run --project Identity.Web.API
```

## 测试与构建

```bash
dotnet build NotBlog.sln            # 全解决方案构建（0 警告 0 错误）
dotnet test NotBlog.sln             # 全部测试（Message.Tests 146 用例）
```
