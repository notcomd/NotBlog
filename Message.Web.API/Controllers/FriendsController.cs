using Message.Domain.Entities;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FriendsController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IFriendProvider _friendProvider;

    public FriendsController(IFriendProvider friendProvider, ICurrentUserService currentUserService)
    {
        _friendProvider = friendProvider;
        _currentUserService = currentUserService;
    }

    [HttpPost("request")]
    public async Task<ActionResult<ApiResponse<FriendDto>>> SendFriendRequest(
        [FromBody] SendFriendRequestRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var friendship = await _friendProvider.SendFriendRequestAsync(userId, request.FriendId);
        return Ok(ApiResponse<FriendDto>.Created(MapToDto(friendship), "好友请求已发送"));
    }

    [HttpPut("request/{friendId}")]
    public async Task<ActionResult<ApiResponse>> HandleFriendRequest(Guid friendId,
        [FromBody] HandleFriendRequestRequest request)
    {
        var userId = _currentUserService.GetUserId();
        if (request.Accept)
            await _friendProvider.AcceptFriendRequestAsync(userId, friendId);
        else
            await _friendProvider.RejectFriendRequestAsync(userId, friendId);

        return Ok(ApiResponse.Ok(request.Accept ? "好友请求已接受" : "好友请求已拒绝"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendDto>>>> GetFriends()
    {
        var userId = _currentUserService.GetUserId();
        var friends = await _friendProvider.GetFriendsAsync(userId);
        return Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
    }

    [HttpGet("requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendRequestDto>>>> GetFriendRequests()
    {
        var userId = _currentUserService.GetUserId();
        var requests = await _friendProvider.GetPendingRequestsAsync(userId);
        return Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
    }

    [HttpGet("sent-requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendRequestDto>>>> GetSentRequests()
    {
        var userId = _currentUserService.GetUserId();
        var requests = await _friendProvider.GetSentRequestsAsync(userId);
        return Ok(ApiResponse<IEnumerable<FriendRequestDto>>.Ok(requests.Select(MapRequestToDto)));
    }

    [HttpDelete("{friendId}")]
    public async Task<ActionResult<ApiResponse>> DeleteFriend(Guid friendId)
    {
        var userId = _currentUserService.GetUserId();
        var friendship = await _friendProvider.GetFriendshipAsync(userId, friendId);
        if (friendship == null)
            return NotFound(ApiResponse.NotFound("好友关系不存在"));

        await _friendProvider.DeleteFriendshipAsync(friendship.FriendshipId);
        return Ok(ApiResponse.Ok("好友已删除"));
    }

    [HttpPut("{friendId}/block")]
    public async Task<ActionResult<ApiResponse>> BlockFriend(Guid friendId, [FromQuery] bool block = true)
    {
        var userId = _currentUserService.GetUserId();
        if (block)
            await _friendProvider.BlockUserAsync(userId, friendId);
        else
            await _friendProvider.UnblockUserAsync(userId, friendId);

        return Ok(ApiResponse.Ok(block ? "用户已屏蔽" : "用户已取消屏蔽"));
    }

    [HttpPut("{friendId}/remark")]
    public async Task<ActionResult<ApiResponse>> UpdateRemark(Guid friendId,
        [FromBody] UpdateFriendRemarkRequest request)
    {
        var userId = _currentUserService.GetUserId();
        await _friendProvider.UpdateFriendRemarkAsync(userId, friendId, request.Remark);
        return Ok(ApiResponse.Ok("备注已更新"));
    }

    [HttpPut("{friendId}/star")]
    public async Task<ActionResult<ApiResponse>> StarFriend(Guid friendId, [FromQuery] bool star = true)
    {
        var userId = _currentUserService.GetUserId();
        if (star)
            await _friendProvider.StarFriendAsync(userId, friendId);
        else
            await _friendProvider.UnstarFriendAsync(userId, friendId);

        return Ok(ApiResponse.Ok(star ? "好友已星标" : "好友已取消星标"));
    }

    [HttpPut("{friendId}/mute")]
    public async Task<ActionResult<ApiResponse>> MuteFriend(Guid friendId, [FromQuery] bool mute = true)
    {
        var userId = _currentUserService.GetUserId();
        if (mute)
            await _friendProvider.MuteFriendAsync(userId, friendId);
        else
            await _friendProvider.UnmuteFriendAsync(userId, friendId);

        return Ok(ApiResponse.Ok(mute ? "好友已静音" : "好友已取消静音"));
    }

    [HttpGet("blocked")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendDto>>>> GetBlockedUsers()
    {
        var userId = _currentUserService.GetUserId();
        var users = await _friendProvider.GetBlockedUsersAsync(userId);
        return Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(users.Select(MapToDto)));
    }

    [HttpGet("starred")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendDto>>>> GetStarredFriends()
    {
        var userId = _currentUserService.GetUserId();
        var friends = await _friendProvider.GetStarredFriendsAsync(userId);
        return Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
    }

    [HttpGet("count")]
    public async Task<ActionResult<ApiResponse<int>>> GetFriendCount()
    {
        var userId = _currentUserService.GetUserId();
        var count = await _friendProvider.GetFriendCountAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IEnumerable<FriendDto>>>> SearchFriends([FromQuery] string searchTerm)
    {
        var userId = _currentUserService.GetUserId();
        var friends = await _friendProvider.SearchFriendsAsync(userId, searchTerm);
        return Ok(ApiResponse<IEnumerable<FriendDto>>.Ok(friends.Select(MapToDto)));
    }

    private static FriendDto MapToDto(MessageFriends friendship) => new()
    {
        FriendshipId = friendship.FriendshipId,
        FriendId = friendship.FriendId,
        Status = friendship.Status,
        Remark = friendship.Remark,
        FriendGroupName = friendship.FriendGroupName,
        IsBlocked = friendship.IsBlocked,
        IsMuted = friendship.IsMuted,
        IsStarred = friendship.IsStarred,
        CreatedTime = friendship.CreatedTime,
        LastInteractionTime = friendship.LastInteractionTime
    };

    private static FriendRequestDto MapRequestToDto(MessageFriends request) => new()
    {
        FriendshipId = request.FriendshipId,
        RequesterId = request.UserId,
        Status = request.Status,
        CreatedTime = request.CreatedTime
    };
}