using CacheMemory.Core;
using Message.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.TestHelpers;

/// <summary>
/// 缓存服务测试辅助工厂（Q-05）：基于 Mock IRedisCacheService 构造 4 个缓存服务实例，
/// 用于直接实例化命令 Handler / MessageHub 的单元测试。
/// </summary>
internal static class CacheServicesTestFactory
{
    public static Mock<IRedisCacheService> CreateRedisMock() => new();

    public static UserStatusCacheService CreateUserStatusCache(Mock<IRedisCacheService>? redis = null)
        => new(redis?.Object ?? new Mock<IRedisCacheService>().Object, new Mock<ILogger<UserStatusCacheService>>().Object);

    public static UnreadCountCacheService CreateUnreadCountCache(Mock<IRedisCacheService>? redis = null)
        => new(redis?.Object ?? new Mock<IRedisCacheService>().Object, new Mock<ILogger<UnreadCountCacheService>>().Object);

    public static SessionCacheService CreateSessionCache(Mock<IRedisCacheService>? redis = null)
        => new(redis?.Object ?? new Mock<IRedisCacheService>().Object, new Mock<ILogger<SessionCacheService>>().Object);

    public static MessageCacheService CreateRedisCache(Mock<IRedisCacheService>? redis = null)
        => new(redis?.Object ?? new Mock<IRedisCacheService>().Object, new Mock<ILogger<MessageCacheService>>().Object);
}
