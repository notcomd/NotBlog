using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Microsoft.AspNetCore.Mvc;

namespace Message.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController(IChatSessionProvider sessionProvider, ICurrentUserService currentUserService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SessionDto>>> CreateSession([FromBody] CreateSessionRequest request)
    {
        var userId = currentUserService.GetUserId();
        ChatSession session;

        if (request.SessionType == SessionType.Private)
        {
            session = await sessionProvider.CreatePrivateSessionAsync(userId, request.FriendId!.Value);
        }
        else
        {
            session = await sessionProvider.CreateGroupSessionAsync(
                request.GroupId!.Value,
                userId,
                request.SessionName!,
                request.InitialMembers ?? new HashSet<Guid>());
        }

        return Ok(ApiResponse<SessionDto>.Created(MapToDto(session), "会话创建成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SessionDto>>>> GetSessions()
    {
        var userId = currentUserService.GetUserId();
        var sessions = await sessionProvider.GetUserSessionsAsync(userId);
        return Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SessionDto>>> GetSession(Guid id)
    {
        var session = await sessionProvider.GetSessionAsync(id);
        if (session == null)
            return NotFound(ApiResponse<SessionDto>.NotFound("会话不存在"));

        return Ok(ApiResponse<SessionDto>.Ok(MapToDto(session)));
    }

    [HttpPut("{id}/pin")]
    public async Task<ActionResult<ApiResponse>> PinSession(Guid id, [FromQuery] bool pin = true)
    {
        if (pin)
            await sessionProvider.PinSessionAsync(id);
        else
            await sessionProvider.UnpinSessionAsync(id);

        return Ok(ApiResponse.Ok(pin ? "会话已置顶" : "会话已取消置顶"));
    }

    [HttpPut("{id}/mute")]
    public async Task<ActionResult<ApiResponse>> MuteSession(Guid id, [FromQuery] bool mute = true)
    {
        if (mute)
            await sessionProvider.MuteSessionAsync(id);
        else
            await sessionProvider.UnmuteSessionAsync(id);

        return Ok(ApiResponse.Ok(mute ? "会话已静音" : "会话已取消静音"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DismissSession(Guid id)
    {
        await sessionProvider.DismissSessionAsync(id);
        return Ok(ApiResponse.Ok("会话已解散"));
    }

    [HttpGet("{id}/participants")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Guid>>>> GetParticipants(Guid id)
    {
        var session = await sessionProvider.GetSessionAsync(id);
        if (session == null)
            return NotFound(ApiResponse<IEnumerable<Guid>>.NotFound("会话不存在"));

        return Ok(ApiResponse<IEnumerable<Guid>>.Ok(session.Participants));
    }

    [HttpPost("{id}/participants")]
    public async Task<ActionResult<ApiResponse>> AddParticipant(Guid id, [FromBody] AddParticipantRequest request)
    {
        await sessionProvider.AddParticipantAsync(id, request.UserId);
        return Ok(ApiResponse.Ok("成员已添加"));
    }

    [HttpDelete("{id}/participants/{userId}")]
    public async Task<ActionResult<ApiResponse>> RemoveParticipant(Guid id, Guid userId)
    {
        await sessionProvider.RemoveParticipantAsync(id, userId);
        return Ok(ApiResponse.Ok("成员已移除"));
    }

    [HttpGet("pinned")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SessionDto>>>> GetPinnedSessions()
    {
        var userId = currentUserService.GetUserId();
        var sessions = await sessionProvider.GetPinnedSessionsAsync(userId);
        return Ok(ApiResponse<IEnumerable<SessionDto>>.Ok(sessions.Select(MapToDto)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTotalUnreadCount()
    {
        var userId = currentUserService.GetUserId();
        var count = await sessionProvider.GetTotalUnreadCountAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    private static SessionDto MapToDto(ChatSession session) => new()
    {
        SessionId = session.SessionId,
        SessionType = session.SessionType,
        SessionName = session.SessionName,
        GroupId = session.GroupId,
        CreatorId = session.CreatorId,
        Participants = session.Participants.ToList(),
        LastMessageId = session.LastMessageId,
        LastMessageContent = session.LastMessageContent,
        LastMessageTime = session.LastMessageTime,
        CreatedTime = session.CreatedTime,
        IsPinned = session.IsPinned,
        IsMuted = session.IsMuted
    };

    public record AddParticipantRequest(Guid UserId);
}