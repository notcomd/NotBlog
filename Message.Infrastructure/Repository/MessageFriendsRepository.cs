using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class MessageFriendsRepository : Repository<MessageFriends>, IMessageFriendsRepository
{
    public MessageFriendsRepository(MessageDbContext context) : base(context)
    {
    }

    public async Task<MessageFriends?> GetByIdAsync(Guid friendshipId)
    {
        return await _dbSet.FirstOrDefaultAsync(f => f.FriendshipId == friendshipId);
    }

    public async Task<MessageFriends?> GetByUserAndFriendAsync(Guid userId, Guid friendId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    public async Task<IEnumerable<MessageFriends>> GetFriendsByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetAcceptedFriendsAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetByStatusAsync(Guid userId, FriendshipStatus status)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.Status == status)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetByFriendGroupAsync(Guid userId, string groupName)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.FriendGroupName == groupName && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId)
    {
        return await _dbSet
            .Where(f => f.UserId == userId && f.IsStarred && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public new async Task<MessageFriends> AddAsync(MessageFriends friendship)
    {
        var entry = await _dbSet.AddAsync(friendship);
        return entry.Entity;
    }

    public new async Task<MessageFriends> UpdateAsync(MessageFriends friendship)
    {
        var entry = _dbSet.Update(friendship);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid friendshipId)
    {
        var friendship = await GetByIdAsync(friendshipId);
        if (friendship != null)
        {
            _dbSet.Remove(friendship);
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid friendId)
    {
        return await _dbSet.AnyAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
    {
        return await _dbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            f.Status == FriendshipStatus.Accepted);
    }

    public async Task<bool> IsBlockedAsync(Guid userId, Guid friendId)
    {
        return await _dbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            (f.Status == FriendshipStatus.Blocked || f.IsBlocked));
    }

    public async Task<int> GetFriendCountAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted);
    }

    public async Task<int> GetPendingRequestCountAsync(Guid userId)
    {
        return await _dbSet
            .CountAsync(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending);
    }

    public async Task AcceptRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship != null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Accept();
            _dbSet.Update(friendship);
        }
    }

    public async Task RejectRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship != null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Reject();
            _dbSet.Update(friendship);
        }
    }

    public async Task BlockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship != null)
        {
            friendship.Block();
            _dbSet.Update(friendship);
        }
    }

    public async Task UnblockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship != null && friendship.IsBlocked)
        {
            friendship.Unblock();
            _dbSet.Update(friendship);
        }
    }

    public async Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm)
    {
        return await _dbSet
            .Where(f => f.UserId == userId &&
                        f.Status == FriendshipStatus.Accepted &&
                        (f.Remark != null && f.Remark.Contains(searchTerm)))
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }
}