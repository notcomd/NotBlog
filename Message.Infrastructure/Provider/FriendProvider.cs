namespace Message.Infrastructure.Provider;

public class FriendProvider : IFriendProvider
{
    private readonly IMessageFriendsRepository _friendRepository;

    private readonly IUnitOfWork _unitOfWork;

    public FriendProvider(
        IMessageFriendsRepository friendRepository,
        IUnitOfWork unitOfWork)
    {
        _friendRepository = friendRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<MessageFriends> SendFriendRequestAsync(Guid userId, Guid friendId)
    {
        return null;
    }

    public async Task AcceptFriendRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(friendId, userId);
        if (friendship == null)
            throw new KeyNotFoundException("好友请求不存在");

        friendship.Accept();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task RejectFriendRequestAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(friendId, userId);
        if (friendship == null)
            throw new KeyNotFoundException("好友请求不存在");

        friendship.Reject();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task<MessageFriends?> GetFriendshipAsync(Guid friendshipId)
    {
        return await _friendRepository.GetByIdAsync(friendshipId);
    }

    public async Task<MessageFriends?> GetFriendshipAsync(Guid userId, Guid friendId)
    {
        return await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
    }

    public async Task<IEnumerable<MessageFriends>> GetFriendsAsync(Guid userId)
    {
        return await _friendRepository.GetAcceptedFriendsAsync(userId);
    }

    public async Task<IEnumerable<MessageFriends>> GetPendingRequestsAsync(Guid userId)
    {
        return await _friendRepository.GetPendingRequestsAsync(userId);
    }

    public async Task<IEnumerable<MessageFriends>> GetSentRequestsAsync(Guid userId)
    {
        return await _friendRepository.GetSentRequestsAsync(userId);
    }

    public async Task<IEnumerable<MessageFriends>> GetBlockedUsersAsync(Guid userId)
    {
        return await _friendRepository.GetBlockedUsersAsync(userId);
    }

    public async Task<IEnumerable<MessageFriends>> GetStarredFriendsAsync(Guid userId)
    {
        return await _friendRepository.GetStarredFriendsAsync(userId);
    }

    public async Task<IEnumerable<MessageFriends>> GetFriendsByGroupAsync(Guid userId, string groupName)
    {
        return await _friendRepository.GetByFriendGroupAsync(userId, groupName);
    }

    public async Task<int> GetFriendCountAsync(Guid userId)
    {
        return await _friendRepository.GetFriendCountAsync(userId);
    }

    public async Task<int> GetPendingRequestCountAsync(Guid userId)
    {
        return await _friendRepository.GetPendingRequestCountAsync(userId);
    }

    public async Task<bool> AreFriendsAsync(Guid userId, Guid friendId)
    {
        return await _friendRepository.AreFriendsAsync(userId, friendId);
    }

    public async Task<bool> IsBlockedAsync(Guid userId, Guid friendId)
    {
        return await _friendRepository.IsBlockedAsync(userId, friendId);
    }

    public async Task<bool> CanSendMessageAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        return friendship?.CanSendMessage() ?? false;
    }

    public async Task BlockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Block();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnblockUserAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Unblock();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task StarFriendAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Star();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnstarFriendAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Unstar();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task MuteFriendAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Mute();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnmuteFriendAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.Unmute();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateFriendRemarkAsync(Guid userId, Guid friendId, string remark)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.UpdateRemark(remark);
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateFriendGroupAsync(Guid userId, Guid friendId, string groupName)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.UpdateFriendGroup(groupName);
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task RecordInteractionAsync(Guid userId, Guid friendId)
    {
        var friendship = await _friendRepository.GetByUserAndFriendAsync(userId, friendId);
        if (friendship == null)
            throw new KeyNotFoundException("好友关系不存在");

        friendship.RecordInteraction();
        await _friendRepository.UpdateAsync(friendship);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task<IEnumerable<MessageFriends>> SearchFriendsAsync(Guid userId, string searchTerm)
    {
        return await _friendRepository.SearchFriendsAsync(userId, searchTerm);
    }

    public async Task DeleteFriendshipAsync(Guid friendshipId)
    {
        await _friendRepository.DeleteAsync(friendshipId);
        await _unitOfWork.SavaEntitiesAsync();
    }
}