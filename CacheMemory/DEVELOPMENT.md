# CacheMemory 开发文档

## 项目概述

CacheMemory 是基于 **StackExchange.Redis 3.0.11** 构建的 .NET Redis 缓存类库，目标框架为 **.NET 10**。提供了完整的 Redis
数据操作、分布式锁、发布订阅、重试策略等企业级功能。

## 目录结构

```
CacheMemory/
├── Core/                              # 核心接口与配置模型
│   ├── IMemory.cs                     # 标记接口（IMemory）
│   ├── ICacheMemory.cs                # 泛型缓存服务接口
│   ├── IRedisCacheService.cs          # Redis 综合操作接口
│   ├── IRedisConnectionProvider.cs    # 连接提供者接口
│   ├── IRedisRetryPolicy.cs           # 重试策略接口
│   ├── CacheMemoryOption.cs           # 全局配置选项 + RedisInstanceOptions
│   └── RetryOptions.cs                # 重试策略配置
├── Providers/                         # 基础设施实现
│   ├── CacheMemoryConfig.cs           # 配置加载器（IConfiguration + 环境变量）
│   ├── CacheMemoryConnection.cs       # 单实例连接管理器（自动重连）
│   ├── RedisConnectionProvider.cs     # 多实例连接提供者
│   └── RedisRetryPolicy.cs            # 指数退避重试策略
├── Service/                           # 业务服务实现
│   ├── RedisCacheService.cs           # Redis 缓存服务（1289 行，virtual 方法）
│   ├── CacheMemory.cs                 # 泛型缓存服务（JSON 序列化）
│   ├── RedisDistributedLock.cs        # 分布式锁（含 LockHandle）
│   └── RedisPubSubService.cs          # 发布/订阅服务
├── Extensions/                        # DI 扩展
│   └── ServiceCollectionExtensions.cs # IServiceCollection 扩展方法
├── CacheMemory.csproj                 # 项目文件
├── GlobalUsings.cs                    # 全局命名空间
├── DEVELOPMENT.md                     # 本文件
└── USAGE.md                           # 使用文档
```

## 架构设计

### 分层架构

```
┌──────────────────────────────────────────────┐
│  调用方（Web API / Application）              │
├──────────────────────────────────────────────┤
│  ICacheMemory<T>    RedisDistributedLock     │  ← 高级服务层
│  RedisPubSubService                          │
├──────────────────────────────────────────────┤
│  IRedisCacheService                          │  ← 核心操作层（virtual）
├──────────────────────────────────────────────┤
│  IRedisConnectionProvider   IRedisRetryPolicy│  ← 基础设施层
├──────────────────────────────────────────────┤
│  StackExchange.Redis                         │  ← 第三方库
└──────────────────────────────────────────────┘
```

### 核心接口职责

| 接口                         | 职责                                                                   |
|----------------------------|----------------------------------------------------------------------|
| `IMemory`                  | 标记接口，带有可选的 `CacheKey` 属性，用于标识 Redis 实体                               |
| `ICacheMemory<TMemory>`    | 泛型缓存服务，自动 JSON 序列化/反序列化                                              |
| `IRedisCacheService`       | Redis 全数据类型操作（String/Hash/List/Set/SortedSet/Geo/HyperLogLog/Bitmap） |
| `IRedisConnectionProvider` | 多实例连接管理，按名称获取 `IConnectionMultiplexer`                               |
| `IRedisRetryPolicy`        | 可配置的指数退避重试策略                                                         |

### 设计模式

- **标记接口模式**：`IMemory` 用于类型约束，确保泛型缓存服务的安全使用
- **策略模式**：`IRedisRetryPolicy` 封装可替换的重试逻辑
- **工厂模式**：`IRedisConnectionProvider` 管理多个 Redis 实例的连接创建
- **模板方法模式**：所有 Service 中的方法均标记为 `virtual`，提供默认实现，允许子类覆写

## 扩展指南

### 自定义重试策略

实现 `IRedisRetryPolicy` 接口后，通过 DI 替换默认实现：

```csharp
services.AddSingleton<IRedisRetryPolicy, MyCustomRetryPolicy>();
```

### 扩展 RedisCacheService

`RedisCacheService` 的所有方法均为 `virtual`，可以通过继承来扩展：

```csharp
public class MyRedisCacheService : RedisCacheService
{
    public MyRedisCacheService(...) : base(...) { }

    public override async Task<string?> StringGetAsync(string key, CancellationToken ct = default)
    {
        // 自定义前置逻辑
        var result = await base.StringGetAsync(key, ct);
        // 自定义后置逻辑
        return result;
    }
}

// 注册
services.AddSingleton<IRedisCacheService, MyRedisCacheService>();
```

### 扩展 CacheMemory<T>

```csharp
public class UserCacheMemory : CacheMemory<UserEntity>
{
    public UserCacheMemory(...) : base(...) { }

    public override async Task<UserEntity?> GetAsync(string key, CancellationToken ct = default)
    {
        // 自定义逻辑
        return await base.GetAsync(key, ct);
    }
}
```

## 重试策略

指数退避公式：\( delay = BaseDelay \times Multiplier^{attempt} \)（上限 `MaxDelay`），叠加 ±25% 随机抖动（Jitter）。

默认值：

- `MaxRetryCount` = 3
- `BaseDelayMilliseconds` = 100ms
- `MaxDelayMilliseconds` = 30s
- `BackoffMultiplier` = 2.0
- `UseJitter` = true

可重试的异常类型：

- `RedisConnectionException`、`RedisTimeoutException`、`RedisException`
- `TimeoutException`、`IOException`、`SocketException`

不可重试：

- `RedisServerException`（包含 WRONGTYPE、NOSCRIPT、NOAUTH 等消息）
- `RedisCommandException`、`ObjectDisposedException`、`OperationCanceledException`

## 配置加载优先级

从高到低：

1. 环境变量：`CacheMemory__Instances__{Name}__ConnectionString`
2. 环境变量：`CacheMemory__ConnectionString`（简化单实例场景）
3. `appsettings.json` 或其他 `IConfiguration` 源
4. 代码默认值（`localhost:6379`）

## 依赖项

| NuGet 包                                               | 版本     | 用途                      |
|-------------------------------------------------------|--------|-------------------------|
| StackExchange.Redis                                   | 3.0.11 | Redis 客户端核心库            |
| Microsoft.Extensions.Caching.StackExchangeRedis       | 10.0.9 | ASP.NET Core Redis 缓存抽象 |
| Microsoft.Extensions.Configuration.Abstractions       | 10.0.9 | 配置抽象                    |
| Microsoft.Extensions.Configuration.Binder             | 10.0.9 | 配置绑定                    |
| Microsoft.Extensions.DependencyInjection              | 10.0.9 | DI 容器                   |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.9 | DI 抽象                   |
| Microsoft.Extensions.Logging.Abstractions             | 10.0.9 | 日志抽象                    |

## 编码规范

- 命名空间：`CacheMemory.Core` / `CacheMemory.Providers` / `CacheMemory.Service` / `CacheMemory.Extensions`
- 接口以 `I` 开头
- 所有异步方法带 `CancellationToken` 参数（默认值为 `default`）
- 异步方法使用 `ConfigureAwait(false)` 避免上下文捕获
- 私有字段以 `_` 前缀命名
- XML 文档注释（`<summary>` / `<param>` / `<returns>` / `<remarks>`）
- 所有 Service 方法标记为 `virtual` 支持覆写
