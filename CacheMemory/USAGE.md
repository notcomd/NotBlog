# CacheMemory 使用文档

## 1. 安装与配置

### 1.1 项目引用

在目标项目的 `.csproj` 中添加项目引用：

```xml

<ItemGroup>
    <ProjectReference Include="..\CacheMemory\CacheMemory.csproj"/>
</ItemGroup>
```

### 1.2 基础配置（appsettings.json）

```json
{
  "CacheMemory": {
    "Instances": {
      "Default": {
        "ConnectionString": "localhost:6379,defaultDatabase=0,connectTimeout=5000",
        "DefaultDatabase": 0,
        "ConnectTimeoutMs": 5000,
        "SyncTimeoutMs": 5000,
        "AllowAdmin": false
      }
    },
    "Retry": {
      "MaxRetryCount": 3,
      "BaseDelayMilliseconds": 100,
      "MaxDelayMilliseconds": 30000,
      "BackoffMultiplier": 2.0,
      "UseJitter": true
    }
  }
}
```

### 1.3 环境变量覆盖

连接字符串等敏感信息推荐通过环境变量覆盖：

```bash
# 覆盖默认实例的连接字符串
export CacheMemory__ConnectionString="redis-server:6379,password=secret"

# 覆盖指定实例
export CacheMemory__Instances__Default__ConnectionString="redis-cluster:6379,password=secret"
export CacheMemory__Instances__Cache__ConnectionString="redis-cache:6379,password=secret2"
```

## 2. DI 注册

### 2.1 Aspire 风格注册（推荐用于 Aspire 项目）

```csharp
// Program.cs - Aspire 项目
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Aspire 原生风格：自动从 ConnectionStrings:Redis 读取连接字符串
builder.AddCacheMemory("Redis");

// 带额外配置
builder.AddCacheMemory("Redis", options =>
{
    options.Retry.MaxRetryCount = 5;
});

// 多实例
builder.AddCacheMemory("Redis", "SessionCache", "AnalyticsCache");

// 纯委托模式
builder.AddCacheMemory(options =>
{
    options.Instances["Default"] = new RedisInstanceOptions
    {
        ConnectionString = "localhost:6379"
    };
});

var app = builder.Build();
```

### 2.2 从配置文件注册（传统 IConfiguration 方式）

```csharp
// Program.cs - 传统项目
var builder = WebApplication.CreateBuilder(args);

// 基础注册（同时检查 Aspire ConnectionStrings 作为补充）
builder.Services.AddCacheMemory(builder.Configuration);

var app = builder.Build();
```

### 2.3 Aspire 兼容的 IServiceCollection 注册

```csharp
// 明确指定 Aspire 连接名称
builder.Services.AddCacheMemory(builder.Configuration, "Redis");
```

### 2.4 从代码委托注册

```csharp
builder.Services.AddCacheMemory(options =>
{
    options.Instances["Default"] = new RedisInstanceOptions
    {
        ConnectionString = "localhost:6379",
        DefaultDatabase = 0
    };
    options.Retry.MaxRetryCount = 5;
});
```

### 2.5 带连接预热的注册（生产环境推荐）

```csharp
// Program.cs 使用顶级语句异步
await builder.Services.AddCacheMemoryWithWarmupAsync(builder.Configuration);
```

## 3. 基本使用

### 3.1 String 操作

```csharp
public class MyService
{
    private readonly IRedisCacheService _redis;

    public MyService(IRedisCacheService redis) => _redis = redis;

    public async Task ExampleAsync(CancellationToken ct = default)
    {
        // 设置字符串（带过期时间）
        await _redis.StringSetAsync("user:1:name", "张三", TimeSpan.FromMinutes(30), ct);

        // 获取字符串
        var name = await _redis.StringGetAsync("user:1:name", ct);

        // 仅当不存在时设置（原子操作）
        var set = await _redis.StringSetIfNotExistsAsync("lock:task1", "locked", TimeSpan.FromSeconds(10), ct);

        // 自增/自减
        var count = await _redis.StringIncrementAsync("counter:visits", 1, ct);
        var dec = await _redis.StringDecrementAsync("stock:item1", 1, ct);

        // 批量操作
        var values = await _redis.StringGetManyAsync(
            new[] { "user:1:name", "user:2:name", "user:3:name" }, ct);
    }
}
```

### 3.2 Hash 操作

```csharp
// 设置单个字段
await _redis.HashSetAsync("user:1", "name", "张三", ct);
await _redis.HashSetAsync("user:1", "email", "zhangsan@example.com", ct);

// 批量设置
await _redis.HashSetManyAsync("user:1", new Dictionary<string, string>
{
    ["name"] = "张三",
    ["email"] = "zhangsan@example.com",
    ["age"] = "28"
}, ct);

// 获取所有字段
var all = await _redis.HashGetAllAsync("user:1", ct);
// all = { "name": "张三", "email": "zhangsan@example.com", "age": "28" }

// 批量获取指定字段
var fields = await _redis.HashGetManyAsync("user:1", new[] { "name", "email" }, ct);

// 数值递增
var newAge = await _redis.HashIncrementAsync("user:1", "age", 1, ct);

// 检查字段是否存在
var exists = await _redis.HashExistsAsync("user:1", "email", ct);
```

### 3.3 List 操作

```csharp
// 左侧推入（头部插入）
await _redis.ListLeftPushAsync("queue:tasks", "task1", ct);
await _redis.ListLeftPushManyAsync("queue:tasks", new[] { "task2", "task3" }, ct);

// 右侧弹出（尾部取出，实现 FIFO 队列）
var task = await _redis.ListRightPopAsync("queue:tasks", ct);

// 右侧推入 + 左侧弹出（阻塞队列）
var moved = await _redis.ListLeftPopRightPushAsync("queue:processing", "queue:done", ct);

// 范围查询
var allTasks = await _redis.ListRangeAsync("queue:tasks", 0, -1, ct);

// 按索引获取
var first = await _redis.ListGetByIndexAsync("queue:tasks", 0, ct);

// 修整列表（保留指定范围）
await _redis.ListTrimAsync("queue:tasks", 0, 99, ct);
```

### 3.4 Set 操作

```csharp
// 添加成员
await _redis.SetAddAsync("tags:article1", "redis", ct);
await _redis.SetAddManyAsync("tags:article1", new[] { "csharp", "cache" }, ct);

// 判断成员是否存在
var hasTag = await _redis.SetContainsAsync("tags:article1", "redis", ct);

// 获取所有成员
var tags = await _redis.SetMembersAsync("tags:article1", ct);

// 集合运算
var common = await _redis.SetCombineAsync(SetOperation.Intersect, "tags:article1", "tags:article2", ct);
var all = await _redis.SetCombineAsync(SetOperation.Union, "tags:article1", "tags:article2", ct);
var diff = await _redis.SetCombineAsync(SetOperation.Difference, "tags:article1", "tags:article2", ct);

// 集合并存储
await _redis.SetCombineAndStoreAsync(SetOperation.Intersect, "tags:common", "tags:article1", "tags:article2", ct);

// 随机成员
var randomTag = await _redis.SetRandomMemberAsync("tags:article1", ct);

// 弹出成员
var popped = await _redis.SetPopAsync("tags:article1", ct);

// 移动成员到另一个集合
await _redis.SetMoveAsync("tags:article1", "tags:archived", "redis", ct);
```

### 3.5 Sorted Set（有序集合）

```csharp
// 添加带分数的成员
await _redis.SortedSetAddAsync("leaderboard", "player1", 100.0, ct);
await _redis.SortedSetAddManyAsync("leaderboard", new[]
{
    ("player2", 200.0),
    ("player3", 150.0),
    ("player4", 300.0)
}, ct);

// 获取分数
var score = await _redis.SortedSetScoreAsync("leaderboard", "player1", ct);

// 排名（从低到高）
var rank = await _redis.SortedSetRankAsync("leaderboard", "player1", Order.Ascending, ct);

// 按排名范围获取
var top3 = await _redis.SortedSetRangeByRankAsync("leaderboard", 0, 2, Order.Descending, ct);

// 按分数范围获取
var highScores = await _redis.SortedSetRangeByScoreAsync("leaderboard", 150, double.PositiveInfinity, ct);

// 增加分数
var newScore = await _redis.SortedSetIncrementAsync("leaderboard", "player1", 50, ct);

// 按排名删除
await _redis.SortedSetRemoveRangeByRankAsync("leaderboard", 0, 9, ct); // 删除后10名
```

### 3.6 Geo 操作

```csharp
// 添加地理位置
await _redis.GeoAddAsync("locations", 116.397128, 39.916527, "Beijing", ct);
await _redis.GeoAddManyAsync("locations", new[]
{
    (121.473701, 31.230416, "Shanghai"),
    (113.264385, 23.129112, "Guangzhou")
}, ct);

// 计算距离
var dist = await _redis.GeoDistanceAsync("locations", "Beijing", "Shanghai", GeoUnit.Kilometers, ct);

// 获取坐标
var positions = await _redis.GeoPositionAsync("locations", new[] { "Beijing", "Shanghai" }, ct);
// positions: [(Beijing, 116.397, 39.916), (Shanghai, 121.474, 31.230)]

// 获取 GeoHash
var hashes = await _redis.GeoHashAsync("locations", new[] { "Beijing" }, ct);

// 按半径搜索
var nearby = await _redis.GeoRadiusAsync("locations", "Beijing", 500, GeoUnit.Kilometers, ct);

// 删除位置
await _redis.GeoRemoveAsync("locations", "Beijing", ct);
```

### 3.7 HyperLogLog（基数统计）

```csharp
// 添加元素
await _redis.HyperLogLogAddAsync("uv:2024-01-01", "user_001", ct);
await _redis.HyperLogLogAddManyAsync("uv:2024-01-01", new[] { "user_002", "user_003" }, ct);

// 获取近似基数
var uvCount = await _redis.HyperLogLogLengthAsync("uv:2024-01-01", ct);

// 合并多个 HyperLogLog
await _redis.HyperLogLogMergeManyAsync("uv:week1", new[] { "uv:2024-01-01", "uv:2024-01-02" }, ct);
```

### 3.8 Bitmap 操作

```csharp
// 设置位
await _redis.BitmapSetAsync("user:signin:2024-01", 0, true, ct);   // 第1天签到
await _redis.BitmapSetAsync("user:signin:2024-01", 14, true, ct);  // 第15天签到

// 获取位
var signed = await _redis.BitmapGetAsync("user:signin:2024-01", 0, ct);

// 统计为1的位数（签到天数）
var signCount = await _redis.BitmapCountAsync("user:signin:2024-01", ct);

// 位运算（AND）
await _redis.BitmapOperationAsync(Bitwise.And, "result", "bitmap1", "bitmap2", ct);
```

### 3.9 Key 管理

```csharp
// 检查键是否存在
var exists = await _redis.KeyExistsAsync("user:1", ct);

// 删除键
var deleted = await _redis.KeyDeleteAsync("user:1", ct);
var count = await _redis.KeyDeleteManyAsync(new[] { "key1", "key2", "key3" }, ct);

// 设置过期时间
await _redis.KeyExpireAsync("session:abc", TimeSpan.FromHours(1), ct);

// 移除过期时间
await _redis.KeyPersistAsync("session:abc", ct);

// 获取剩余 TTL
var ttl = await _redis.KeyTtlAsync("session:abc", ct);

// 获取键类型
var type = await _redis.KeyTypeAsync("user:1", ct); // RedisType.String

// 重命名
await _redis.KeyRenameAsync("old:key", "new:key", ct);

// 按模式搜索
var keys = await _redis.KeysByPatternAsync("user:*", pageSize: 100, ct);
```

## 4. 泛型缓存服务（ICacheMemory<T>）

### 4.1 定义实体

```csharp
public class UserEntity : IMemory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // 实现 CacheKey 属性，自动使用 Id 作为缓存键
    public string? CacheKey => $"user:{Id}";
}
```

### 4.2 注入并使用

```csharp
public class UserService
{
    private readonly ICacheMemory<UserEntity> _cache;

    public UserService(ICacheMemory<UserEntity> cache) => _cache = cache;

    public async Task<UserEntity?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        // 手动指定键
        return await _cache.GetAsync($"user:{userId}", ct);
    }

    public async Task SetUserAsync(UserEntity user, CancellationToken ct = default)
    {
        // 使用实体的 CacheKey
        await _cache.SetByEntityAsync(user, TimeSpan.FromMinutes(30), ct);
    }

    public async Task<UserEntity?> GetByEntityAsync(UserEntity user, CancellationToken ct = default)
    {
        return await _cache.GetByEntityAsync(user, ct);
    }

    public async Task<IEnumerable<UserEntity?>> GetUsersAsync(
        IEnumerable<string> userIds, CancellationToken ct = default)
    {
        var keys = userIds.Select(id => $"user:{id}");
        return await _cache.GetManyAsync(keys, ct);
    }

    public async Task RemoveUserAsync(string userId, CancellationToken ct = default)
    {
        await _cache.RemoveAsync($"user:{userId}", ct);
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken ct = default)
    {
        return await _cache.ExistsAsync($"user:{userId}", ct);
    }
}
```

## 5. 分布式锁

> 锁支持**可重入**：同一异步执行流内可对同一把锁重复获取（内部维护重入计数，不会重复执行 SET NX）；
> 释放时仅递减计数，计数归零才会真正删除 Redis 中的锁键。跨进程/实例之间仍然互斥。

```csharp
public class OrderService
{
    private readonly RedisDistributedLock _distributedLock;

    public OrderService(RedisDistributedLock distributedLock) => _distributedLock = distributedLock;

    public async Task ProcessOrderAsync(string orderId, CancellationToken ct = default)
    {
        var lockKey = $"lock:order:{orderId}";

        // 方式1：使用 using 自动释放
        await using (var handle = await _distributedLock.AcquireAsync(lockKey, TimeSpan.FromSeconds(30), ct))
        {
            if (handle == null)
            {
                // 获取锁失败，可能其他进程正在处理
                throw new InvalidOperationException("订单正在处理中，请稍后重试");
            }

            // 执行业务逻辑...
            await DoOrderProcessing(orderId, ct);

            // 长时间操作可以续期
            if (needMoreTime)
            {
                await handle.ExtendAsync(TimeSpan.FromSeconds(30), ct);
            }

        } // 自动释放锁

        // 方式2：带重试的获取
        var handle2 = await _distributedLock.AcquireWithRetryAsync(
            lockKey,
            TimeSpan.FromSeconds(30),
            maxRetries: 5,
            retryDelay: TimeSpan.FromMilliseconds(200),
            cancellationToken: ct);

        if (handle2 != null)
        {
            try { /* 业务逻辑 */ }
            finally { await handle2.DisposeAsync(); }
        }
    }
}
```

## 6. 发布/订阅

```csharp
// --- 发布端 ---
public class NotificationService
{
    private readonly RedisPubSubService _pubSub;

    public NotificationService(RedisPubSubService pubSub) => _pubSub = pubSub;

    public async Task SendNotificationAsync(string message, CancellationToken ct = default)
    {
        // 发布字符串消息
        var subscriberCount = await _pubSub.PublishAsync("channel:notifications", message, ct);

        // 发布序列化对象
        var notification = new { Title = "新消息", Content = message, Time = DateTime.UtcNow };
        await _pubSub.PublishObjectAsync("channel:notifications", notification, ct);
    }
}

// --- 订阅端 ---
public class NotificationListener : IHostedService
{
    private readonly RedisPubSubService _pubSub;
    private readonly ILogger<NotificationListener> _logger;

    public NotificationListener(RedisPubSubService pubSub, ILogger<NotificationListener> logger)
    {
        _pubSub = pubSub;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await _pubSub.SubscribeAsync("channel:notifications", (channel, message) =>
        {
            _logger.LogInformation("收到消息 [{Channel}]: {Message}", channel, message);
        }, ct);
    }

    public Task StopAsync(CancellationToken ct)
    {
        return _pubSub.UnsubscribeAsync("channel:notifications", ct);
    }
}
```

## 7. Pipeline / Batch / Transaction

```csharp
public class BatchService
{
    private readonly IRedisCacheService _redis;

    public BatchService(IRedisCacheService redis) => _redis = redis;

    // Pipeline（批量发送，不保证原子性，不等待每个命令的结果）
    public async Task BatchExampleAsync(CancellationToken ct = default)
    {
        var batch = _redis.CreateBatch();
        var setTask = batch.StringSetAsync("batch:key1", "value1");
        var getTask = batch.StringGetAsync("batch:key2");
        var incrTask = batch.StringIncrementAsync("batch:counter");

        await _redis.ExecuteBatchAsync(batch, ct);

        // 此时可以获取结果
        var value = await getTask;
    }

    // Transaction（保证原子性，通过 MULTI/EXEC）
    public async Task TransactionExampleAsync(CancellationToken ct = default)
    {
        var tran = _redis.CreateTransaction();
        tran.AddCondition(Condition.KeyExists("key1")); // 乐观锁条件
        var setTask = tran.StringSetAsync("key1", "new_value");
        var getTask = tran.StringGetAsync("key2");

        var committed = await tran.ExecuteAsync();
        if (committed)
        {
            var result = await getTask;
        }
    }
}
```

## 8. Lua 脚本

```csharp
// 执行简单 Lua 脚本
var result = await _redis.ScriptEvaluateAsync(
    "return redis.call('GET', KEYS[1])",
    new RedisKey[] { "mykey" },
    null, ct);

// 使用预加载脚本（性能更优）
var script = LuaScript.Prepare("return redis.call('INCR', @key)");
var loaded = await script.LoadAsync(server);
var incrResult = await _redis.ScriptEvaluateAsync(loaded, new { key = (RedisKey)"counter" }, ct);
```

## 9. 多实例配置

```json
{
  "CacheMemory": {
    "Instances": {
      "Default": {
        "ConnectionString": "localhost:6379,defaultDatabase=0"
      },
      "Cache": {
        "ConnectionString": "cache-server:6379,defaultDatabase=0,password=cachepass"
      },
      "Session": {
        "ConnectionString": "session-server:6379,defaultDatabase=0,password=sessionpass"
      }
    }
  }
}
```

通过 `IRedisConnectionProvider` 按名称获取连接：

```csharp
var provider = serviceProvider.GetRequiredService<IRedisConnectionProvider>();
var cacheConn = provider.GetConnection("Cache");       // 获取 Cache 实例连接
var sessionConn = provider.GetConnection("Session");   // 获取 Session 实例连接
var names = provider.GetInstanceNames();               // ["Default", "Cache", "Session"]
```

## 10. 重试策略配置

```json
{
  "CacheMemory": {
    "Retry": {
      "MaxRetryCount": 3,
      "BaseDelayMilliseconds": 100,
      "MaxDelayMilliseconds": 30000,
      "BackoffMultiplier": 2.0,
      "UseJitter": true
    },
    "Instances": {
      "Default": {
        "ConnectionString": "localhost:6379",
        "Retry": {
          "MaxRetryCount": 5,
          "BaseDelayMilliseconds": 200
        }
      }
    }
  }
}
```

每个实例可以独立配置重试策略，未配置时使用全局策略。

## 11. 工具方法

```csharp
// Ping 检查连接
var latency = await _redis.PingAsync(ct);
Console.WriteLine($"Redis 延迟: {latency.TotalMilliseconds}ms");

// 清空数据库（需 AllowAdmin = true）
await _redis.FlushDatabaseAsync(db: 0, ct);
```

## 12. Aspire 集成详解

### 12.1 AppHost 配置

在 `NotBlog.AppHost` 中注册 Redis：

```csharp
// AppHost/AppHost.cs
var redis = builder.AddRedis("Redis");

// 将 Redis 引用传递给各个微服务
builder.AddProject<Projects.Identity_Web_API>("identity-web-api")
    .WithReference(redis);
```

### 12.2 服务端使用

```csharp
// Identity.Web.API/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddCacheMemory("Redis");  // 自动读取 ConnectionStrings:Redis
```

### 12.3 健康检查

CacheMemory 自动注册 `CacheMemoryHealthCheck`（标签 `["redis", "cache"]`），
与 Aspire 的 `/health` 和 `/alive` 端点无缝集成：

```bash
# 查看健康状态
curl https://localhost:5001/health

# 仅检查存活状态（含 Redis）
curl https://localhost:5001/alive
```

健康检查会 Ping 所有已注册的 Redis 实例，返回聚合状态：

| 状态          | 含义       |
|-------------|----------|
| `Healthy`   | 所有实例连接正常 |
| `Degraded`  | 部分实例连接失败 |
| `Unhealthy` | 全部实例连接失败 |

### 12.4 ConnectionStrings 配置格式

Aspire 自动在 `appsettings.json` 中注入连接字符串：

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

多个 Redis 实例：

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379",
    "SessionCache": "session-redis:6379,password=xxx",
    "AnalyticsCache": "analytics-redis:6379,password=xxx"
  }
}
```

## 13. 同步 API

所有异步方法均提供对应的同步重载，适用于无法使用异步的场景：

```csharp
// 同步 String 操作
_redis.StringSet("key", "value", TimeSpan.FromMinutes(30));
var value = _redis.StringGet("key");

// 同步 Hash 操作
_redis.HashSet("user:1", "name", "张三");
var all = _redis.HashGetAll("user:1");

// 同步锁
var handle = _distributedLock.Acquire("lock:task", TimeSpan.FromSeconds(30));

// 同步泛型缓存
_cache.Set("user:1", userEntity);
var user = _cache.Get("user:1");
```
