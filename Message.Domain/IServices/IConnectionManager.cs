namespace Message.Domain.IServices;

public interface IConnectionManager
{
    Task AddConnectionAsync(Guid userId, string connectionId);
    Task RemoveConnectionAsync(Guid userId, string connectionId);
    Task<IEnumerable<string>> GetConnectionsAsync(Guid userId);
    Task<bool> HasOtherConnectionsAsync(Guid userId);
    Task SetUserOnlineAsync(Guid userId);
    Task SetUserOfflineAsync(Guid userId);
    Task<bool> IsUserOnlineAsync(Guid userId);
    Task<int> GetOnlineCountAsync();
}