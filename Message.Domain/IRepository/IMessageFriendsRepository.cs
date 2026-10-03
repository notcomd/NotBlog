
namespace Message.Domain.IRepository;

/// <summary>
/// 好友关系仓储接口（MessageFriends 聚合根）。
/// </summary>
public interface IMessageFriendsRepository  : IRepository<MessageFriends, IUnitOfWork>
{
    /// <summary>按好友关系 ID 查询（不存在返回 null）</summary>
    Task<MessageFriends?> GetByIdAsync(Guid friendshipId);
    /// <summary>查询两人之间的好友关系（不存在返回 null）</summary>
    Task<MessageFriends?> GetByUserAndFriendAsync(Guid userId, Guid friendId);
    /// <summary>获取用户的好友列表</summary>
    Task<IEnumerable<MessageFriends>> GetFriendsByUserIdAsync(Guid userId);
    /// <summary>获取用户收到的好友请求</summary>
    Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId);
    /// <summary>获取用户发出的好友请求</summary>
    Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId);
    /// <summary>获取用户已通过的好友</summary>
    Task<IEnumerable<MessageFriends>> GetAcceptedFriendsAsync(Guid userId);
    /// <summary>获取用户拉黑的用户列表</summary>
    Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId);
    /// <summary>按状态查询用户的好友关系</summary>
    Task<IEnumerable<MessageFriends>> GetByStatusAsync(Guid userId, FriendshipStatus status);
    /// <summary>按好友分组名称查询好友</summary>
    Task<IEnumerable<MessageFriends>> GetByFriendGroupAsync(Guid userId, string groupName);
    /// <summary>获取用户的星标好友</summary>
    Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId);

    /// <summary>
    /// 获取全部好友用户ID（双向 Accepted：我发起的 + 对方发起的；R-09 在线状态推送用）。
    /// </summary>
    Task<IEnumerable<Guid>> GetFriendIdsAsync(Guid userId);
    /// <summary>新增好友关系</summary>
    Task<MessageFriends> AddAsync(MessageFriends friendship);
    /// <summary>更新好友关系</summary>
    Task<MessageFriends> UpdateAsync(MessageFriends friendship);
    /// <summary>删除好友关系</summary>
    Task DeleteAsync(Guid friendshipId);
    /// <summary>判断好友关系是否存在</summary>
    Task<bool> ExistsAsync(Guid userId, Guid friendId);
    /// <summary>判断两人是否为好友</summary>
    Task<bool> AreFriendsAsync(Guid userId, Guid friendId);
    /// <summary>判断用户是否已拉黑对方</summary>
    Task<bool> IsBlockedAsync(Guid userId, Guid friendId);
    /// <summary>获取用户的好友数量</summary>
    Task<int> GetFriendCountAsync(Guid userId);
    /// <summary>获取用户的待处理好友请求数量</summary>
    Task<int> GetPendingRequestCountAsync(Guid userId);
    /// <summary>接受好友请求</summary>
    Task AcceptRequestAsync(Guid userId, Guid friendId);
    /// <summary>拒绝好友请求</summary>
    Task RejectRequestAsync(Guid userId, Guid friendId);
    /// <summary>拉黑用户</summary>
    Task BlockUserAsync(Guid userId, Guid friendId);
    /// <summary>解除拉黑</summary>
    Task UnblockUserAsync(Guid userId, Guid friendId);
    /// <summary>搜索好友</summary>
    Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm);
}