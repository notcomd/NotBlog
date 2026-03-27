namespace Message.Domain.IServices;

public interface IUserStatusCacheService
{
    Task SetUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetUserOfflineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserOnlineAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastOnlineTimeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetOnlineUserCountAsync(CancellationToken cancellationToken = default);
}

public interface ISessionCacheService
{
    Task CacheSessionAsync(Guid sessionId, object sessionInfo, CancellationToken cancellationToken = default);
    Task<T?> GetSessionAsync<T>(Guid sessionId, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SetLastMessageAsync(Guid sessionId, Guid messageId, string content,
        CancellationToken cancellationToken = default);
}

public interface IUnreadCountCacheService
{
    Task IncrementUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task DecrementUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, int>> GetAllUnreadCountsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ClearUnreadCountAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<int> GetTotalUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}