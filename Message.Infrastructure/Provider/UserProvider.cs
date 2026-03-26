using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.SeedWork;

namespace Message.Infrastructure.Provider;

public class UserProvider(
    IUserRepository userRepository,
    IMessageFriendsRepository friendRepository,
    IUnitOfWork unitOfWork)
    : IUserProvider
{
    public async Task<User> RegisterAsync(string userName, string? email, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("用户名不能为空", nameof(userName));

        if (await userRepository.UserNameExistsAsync(userName))
            throw new InvalidOperationException("用户名已存在");

        if (!string.IsNullOrEmpty(email) && await userRepository.EmailExistsAsync(email))
            throw new InvalidOperationException("邮箱已被注册");

        var user = new User(Guid.NewGuid(), userName)
        {
            Email = email,
            PhoneNumber = phoneNumber
        };

        await userRepository.AddAsync(user);
        await unitOfWork.SavaEntitiesAsync();

        return user;
    }

    public async Task<bool> ValidateUserAsync(Guid userId)
    {
        return await userRepository.ExistsAsync(userId);
    }

    public async Task<User?> GetUserProfileAsync(Guid userId)
    {
        return await userRepository.GetByIdAsync(userId);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await userRepository.GetByUserNameAsync(userName);
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds)
    {
        return await userRepository.GetByIdsAsync(userIds);
    }

    public async Task SetUserOnlineAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        user.SetOnline();
        await userRepository.UpdateAsync(user);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task SetUserOfflineAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        user.SetOffline();
        await userRepository.UpdateAsync(user);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task SetUserStatusAsync(Guid userId, UserStatus status)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        user.SetStatus(status);
        await userRepository.UpdateAsync(user);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task<IEnumerable<User>> GetOnlineUsersAsync()
    {
        return await userRepository.GetOnlineUsersAsync();
    }

    public async Task<IEnumerable<User>> GetOnlineFriendsAsync(Guid userId)
    {
        var friends = await friendRepository.GetAcceptedFriendsAsync(userId);
        var friendIds = friends.Select(f => f.FriendId).ToList();

        if (!friendIds.Any())
            return Enumerable.Empty<User>();

        var users = await userRepository.GetByIdsAsync(friendIds);
        return users.Where(u => u.Status == UserStatus.Online);
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page, int pageSize)
    {
        return await userRepository.SearchAsync(searchTerm, page, pageSize);
    }

    public async Task<bool> UserNameExistsAsync(string userName)
    {
        return await userRepository.UserNameExistsAsync(userName);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await userRepository.EmailExistsAsync(email);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await userRepository.GetTotalCountAsync();
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        user.Delete();
        await userRepository.UpdateAsync(user);
        await unitOfWork.SavaEntitiesAsync();
    }
}