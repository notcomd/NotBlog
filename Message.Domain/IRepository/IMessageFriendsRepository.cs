using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.IRepository;

public interface IMessageFriendsRepository 
{
    Task<MessageFriends?> GetByIdAsync(Guid friendshipId);
    Task<MessageFriends?> GetByUserAndFriendAsync(Guid userId, Guid friendId);
    Task<IEnumerable<MessageFriends>> GetFriendsByUserIdAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetAcceptedFriendsAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId);
    Task<IEnumerable<MessageFriends>> GetByStatusAsync(Guid userId, FriendshipStatus status);
    Task<IEnumerable<MessageFriends>> GetByFriendGroupAsync(Guid userId, string groupName);
    Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId);
    Task<MessageFriends> AddAsync(MessageFriends friendship);
    Task<MessageFriends> UpdateAsync(MessageFriends friendship);
    Task DeleteAsync(Guid friendshipId);
    Task<bool> ExistsAsync(Guid userId, Guid friendId);
    Task<bool> AreFriendsAsync(Guid userId, Guid friendId);
    Task<bool> IsBlockedAsync(Guid userId, Guid friendId);
    Task<int> GetFriendCountAsync(Guid userId);
    Task<int> GetPendingRequestCountAsync(Guid userId);
    Task AcceptRequestAsync(Guid userId, Guid friendId);
    Task RejectRequestAsync(Guid userId, Guid friendId);
    Task BlockUserAsync(Guid userId, Guid friendId);
    Task UnblockUserAsync(Guid userId, Guid friendId);
    Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm);
}