
namespace Message.Infrastructure.Repository;

public class MessageFriendsRepository(MessageDbContext context)
    : IMessageFriendsRepository
{
   
    public IUnitOfWork UnitOfWork => context;
    private readonly DbSet<MessageFriends> DbSet = context.MessageFriends;

    public async Task<MessageFriends?> GetByIdAsync(Guid friendshipId)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.FriendshipId == friendshipId);
    }

    public async Task<MessageFriends?> GetByUserAndFriendAsync(Guid userId, Guid friendId)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    public async Task<IEnumerable<MessageFriends>> GetFriendsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetAcceptedFriendsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.Status == FriendshipStatus.Accepted
                        && (f.UserId == userId || f.FriendId == userId))
            .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetByStatusAsync(Guid userId, FriendshipStatus status)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == status)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetByFriendGroupAsync(Guid userId, string groupName)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.FriendGroupName == groupName && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.IsStarred && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    public async Task<MessageFriends> AddAsync(MessageFriends friendship)
    {
        var entry = await DbSet.AddAsync(friendship);
        return entry.Entity;
    }

    public async Task<MessageFriends> UpdateAsync(MessageFriends friendship)
    {
        var entry = DbSet.Update(friendship);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid friendshipId)
    {
        var friendship = await GetByIdAsync(friendshipId);
        if (friendship is not null)
        {
            DbSet.Remove(friendship);
        }
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            f.Status == FriendshipStatus.Accepted);
    }

    public async Task<bool> IsBlockedAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            (f.Status == FriendshipStatus.Blocked || f.IsBlocked));
    }

    public async Task<int> GetFriendCountAsync(Guid userId)
    {
        return await DbSet
            .CountAsync(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted);
    }

    public async Task<int> GetPendingRequestCountAsync(Guid userId)
    {
        return await DbSet
            .CountAsync(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending);
    }

    public async Task AcceptRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship is not null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Accept();
            DbSet.Update(friendship);
        }
    }

    public async Task RejectRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship is not null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Reject();
            DbSet.Update(friendship);
        }
    }

    public async Task BlockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship is not null)
        {
            friendship.Block();
            DbSet.Update(friendship);
        }
    }

    public async Task UnblockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship is not null && friendship.IsBlocked)
        {
            friendship.Unblock();
            DbSet.Update(friendship);
        }
    }

    public async Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm)
    {
        return await DbSet
            .Where(f => f.UserId == userId &&
                        f.Status == FriendshipStatus.Accepted &&
                        (f.Remark != null && f.Remark.Contains(searchTerm)))
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }
}