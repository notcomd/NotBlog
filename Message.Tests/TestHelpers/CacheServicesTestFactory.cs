using Message.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Message.Tests.TestHelpers;

/// <summary>
/// 缓存服务测试辅助工厂（Q-05）：基于 Mock Redis 构造 4 个缓存服务实例，
/// 用于直接实例化命令 Handler / MessageHub 的单元测试。
/// </summary>
internal static class CacheServicesTestFactory
{
    public static Mock<IDatabase> CreateDatabaseMock() => new();

    public static Mock<IConnectionMultiplexer> CreateRedisMock(Mock<IDatabase>? database = null)
    {
        var db = database ?? new Mock<IDatabase>();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        return redis;
    }

    public static UserStatusCacheService CreateUserStatusCache(Mock<IDatabase>? database = null)
        => new(CreateRedisMock(database).Object, new Mock<ILogger<UserStatusCacheService>>().Object);

    public static UnreadCountCacheService CreateUnreadCountCache(Mock<IDatabase>? database = null)
        => new(CreateRedisMock(database).Object, new Mock<ILogger<UnreadCountCacheService>>().Object);

    public static SessionCacheService CreateSessionCache(Mock<IDatabase>? database = null)
        => new(CreateRedisMock(database).Object, new Mock<ILogger<SessionCacheService>>().Object);

    public static  MessageCacheService CreateRedisCache(Mock<IDatabase>? database = null)
        => new(CreateRedisMock(database).Object, new Mock<ILogger< MessageCacheService>>().Object);
}
