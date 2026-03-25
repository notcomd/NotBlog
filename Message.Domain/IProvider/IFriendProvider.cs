using Message.Domain.Entities;

namespace Message.Domain.IProvider;

public interface IFriendProvider
{
    Task<MessageFriends> SendFriendRequestAsync(Guid userId, Guid friendId);
    Task AcceptFriendRequestAsync(Guid userId, Guid friendId);
    Task RejectFriendRequestAsync(Guid userId, Guid friendId);

    Task<MessageFriends?> GetFriendshipAsync(Guid friendshipId);
    Task<MessageFriends?> GetFriendshipAsync(Guid userId, Guid friendId);
    Task<IEnumerable<MessageFriends>> GetFriendsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetFriendsByGroupAsync(Guid userId, string groupName);

    Task<int> GetFriendCountAsync(Guid userId);
    Task<int> GetPendingRequestCountAsync(Guid userId);
    Task<bool> AreFriendsAsync(Guid userId, Guid friendId);
    Task<bool> IsBlockedAsync(Guid userId, Guid friendId);
    Task<bool> CanSendMessageAsync(Guid userId, Guid friendId);

    Task BlockUserAsync(Guid userId, Guid friendId);
    Task UnblockUserAsync(Guid userId, Guid friendId);
    Task StarFriendAsync(Guid userId, Guid friendId);
    Task UnstarFriendAsync(Guid userId, Guid friendId);
    Task MuteFriendAsync(Guid userId, Guid friendId);
    Task UnmuteFriendAsync(Guid userId, Guid friendId);

    Task UpdateFriendRemarkAsync(Guid userId, Guid friendId, string remark);
    Task UpdateFriendGroupAsync(Guid userId, Guid friendId, string groupName);
    Task RecordInteractionAsync(Guid userId, Guid friendId);

    Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm);
    Task DeleteFriendshipAsync(Guid friendshipId);
}