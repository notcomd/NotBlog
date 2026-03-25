using Message.Domain.Entities;
using Message.Domain.Enums;

namespace Message.Domain.IProvider;

public interface IUserProvider
{
    Task<User> RegisterAsync(string userName, string? email, string? phoneNumber);
    Task<bool> ValidateUserAsync(Guid userId);
    Task<User?> GetUserProfileAsync(Guid userId);
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds);
    Task SetUserOnlineAsync(Guid userId);
    Task SetUserOfflineAsync(Guid userId);
    Task SetUserStatusAsync(Guid userId, UserStatus status);
    Task<IEnumerable<User>> GetOnlineUsersAsync();
    Task<IEnumerable<User>> GetOnlineFriendsAsync(Guid userId);
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page, int pageSize);
    Task<bool> UserNameExistsAsync(string userName);
    Task<bool> EmailExistsAsync(string email);
    Task<int> GetTotalCountAsync();
    Task DeleteUserAsync(Guid userId);
}