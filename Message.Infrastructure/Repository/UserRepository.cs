using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class UserRepository(MessageDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.UserName == userName);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<Guid> userIds)
    {
        var ids = userIds.ToList();
        return await DbSet.Where(u => ids.Contains(u.UserId)).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetOnlineUsersAsync()
    {
        return await DbSet.Where(u => u.Status == UserStatus.Online).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetUsersByStatusAsync(UserStatus status)
    {
        return await DbSet.Where(u => u.Status == status).ToListAsync();
    }

    public new async Task<User> AddAsync(User user)
    {
        var entry = await DbSet.AddAsync(user);
        return entry.Entity;
    }

    public new async Task<User> UpdateAsync(User user)
    {
        var entry = DbSet.Update(user);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid userId)
    {
        var user = await GetByIdAsync(userId);
        if (user != null)
        {
            user.Delete();
            DbSet.Update(user);
        }
    }

    public async Task<bool> ExistsAsync(Guid userId)
    {
        return await DbSet.AnyAsync(u => u.UserId == userId);
    }

    public async Task<bool> UserNameExistsAsync(string userName)
    {
        return await DbSet.AnyAsync(u => u.UserName == userName);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await DbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await DbSet.CountAsync();
    }

    public async Task<IEnumerable<User>> SearchAsync(string searchTerm, int page, int pageSize)
    {
        var query = DbSet.Where(u =>
            u.UserName.Contains(searchTerm) ||
            (u.NickName != null && u.NickName.Contains(searchTerm)));

        return await ApplyPaging(query, page, pageSize).ToListAsync();
    }
}