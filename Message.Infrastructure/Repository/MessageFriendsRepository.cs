
namespace Message.Infrastructure.Repository;

/// <summary>好友关系仓储实现，负责 MessageFriends 表的查询与持久化。</summary>
public class MessageFriendsRepository(MessageDbContext context)
    : IMessageFriendsRepository
{
   
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;
    private readonly DbSet<MessageFriends> DbSet = context.MessageFriends;

    /// <summary>按好友关系 ID 获取记录，不存在时返回 null。</summary>
    public async Task<MessageFriends?> GetByIdAsync(Guid friendshipId)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.FriendshipId == friendshipId);
    }

    /// <summary>获取指定用户与指定好友之间的关系记录，不存在时返回 null。</summary>
    public async Task<MessageFriends?> GetByUserAndFriendAsync(Guid userId, Guid friendId)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    /// <summary>获取指定用户的好友列表，按最后互动时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetFriendsByUserIdAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    /// <summary>获取指定用户收到的好友请求，按创建时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    /// <summary>获取指定用户发出的好友请求，按创建时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    /// <summary>获取指定用户已接受的好友，按最后互动时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetAcceptedFriendsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    /// <summary>获取与指定用户已互为好友的好友 ID 集合。</summary>
    public async Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.Status == FriendshipStatus.Accepted
                        && (f.UserId == userId || f.FriendId == userId))
            .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>获取指定用户拉黑的用户列表。</summary>
    public async Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Blocked)
            .ToListAsync();
    }

    /// <summary>按好友状态获取指定用户的关系记录，按创建时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetByStatusAsync(Guid userId, FriendshipStatus status)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.Status == status)
            .OrderByDescending(f => f.CreatedTime)
            .ToListAsync();
    }

    /// <summary>获取指定用户在指定好友分组内的已接受好友，按最后互动时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetByFriendGroupAsync(Guid userId, string groupName)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.FriendGroupName == groupName && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    /// <summary>获取指定用户标记为星标的好友，按最后互动时间倒序。</summary>
    public async Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId)
    {
        return await DbSet
            .Where(f => f.UserId == userId && f.IsStarred && f.Status == FriendshipStatus.Accepted)
            .OrderByDescending(f => f.LastInteractionTime)
            .ToListAsync();
    }

    /// <summary>新增好友关系并返回已跟踪的实体。</summary>
    public async Task<MessageFriends> AddAsync(MessageFriends friendship)
    {
        var entry = await DbSet.AddAsync(friendship);
        return entry.Entity;
    }

    /// <summary>更新好友关系并返回已跟踪的实体。</summary>
    public async Task<MessageFriends> UpdateAsync(MessageFriends friendship)
    {
        var entry = DbSet.Update(friendship);
        return entry.Entity;
    }

    /// <summary>删除指定好友关系。</summary>
    public async Task DeleteAsync(Guid friendshipId)
    {
        var friendship = await GetByIdAsync(friendshipId);
        if (friendship is not null)
        {
            DbSet.Remove(friendship);
        }
    }

    /// <summary>判断指定好友关系是否存在（任意状态）。</summary>
    public async Task<bool> ExistsAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f => f.UserId == userId && f.FriendId == friendId);
    }

    /// <summary>判断指定用户与指定好友是否已互为好友。</summary>
    public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            f.Status == FriendshipStatus.Accepted);
    }

    /// <summary>判断指定用户是否已拉黑指定好友。</summary>
    public async Task<bool> IsBlockedAsync(Guid userId, Guid friendId)
    {
        return await DbSet.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendId == friendId &&
            f.Status == FriendshipStatus.Blocked);
    }

    /// <summary>统计指定用户已接受的好友数量。</summary>
    public async Task<int> GetFriendCountAsync(Guid userId)
    {
        return await DbSet
            .CountAsync(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted);
    }

    /// <summary>统计指定用户收到的好友请求数量。</summary>
    public async Task<int> GetPendingRequestCountAsync(Guid userId)
    {
        return await DbSet
            .CountAsync(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending);
    }

    /// <summary>接受指定用户收到的好友请求。</summary>
    public async Task AcceptRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship is not null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Accept();
            DbSet.Update(friendship);
        }
    }

    /// <summary>拒绝指定用户收到的好友请求。</summary>
    public async Task RejectRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(friendId, userId);
        if (friendship is not null && friendship.Status == FriendshipStatus.Pending)
        {
            friendship.Reject();
            DbSet.Update(friendship);
        }
    }

    /// <summary>拉黑指定好友。</summary>
    public async Task BlockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship is not null)
        {
            friendship.Block();
            DbSet.Update(friendship);
        }
    }

    /// <summary>解除对指定好友的拉黑。</summary>
    public async Task UnblockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await GetByUserAndFriendAsync(userId, friendId);
        if (friendship is not null && friendship.IsBlocked)
        {
            friendship.Unblock();
            DbSet.Update(friendship);
        }
    }

    /// <summary>在已接受的好友中按备注搜索，按最后互动时间倒序。</summary>
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