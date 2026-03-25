using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByUserNameAsync(string userName);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds);
    Task<IEnumerable<User>> GetOnlineUsersAsync();
    Task<IEnumerable<User>> GetUsersByStatusAsync(UserStatus status);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId);
    Task<bool> UserNameExistsAsync(string userName);
    Task<bool> EmailExistsAsync(string email);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<User>> SearchAsync(string searchTerm, int page, int pageSize);
}