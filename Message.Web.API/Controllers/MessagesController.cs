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
public class MessagesController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageProvider _messageProvider;

    public MessagesController(IMessageProvider messageProvider, ICurrentUserService currentUserService)
    {
        _messageProvider = messageProvider;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var message = request.MessageType switch
        {
            MessageType.MessageText => await _messageProvider.SendTextMessageAsync(request.SessionId, userId,
                request.Content ?? ""),
            MessageType.MessageImage => await _messageProvider.SendImageMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Caption, request.ThumbnailUrl),
            MessageType.MessageVideo => await _messageProvider.SendVideoMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption, request.ThumbnailUrl),
            MessageType.MessageAudio => await _messageProvider.SendAudioMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption),
            MessageType.MessageFile => await _messageProvider.SendFileMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.FileName!, request.FileSize ?? 0, request.MimeType!),
            MessageType.MessageLocation => await _messageProvider.SendLocationMessageAsync(request.SessionId, userId,
                request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName!),
            MessageType.MessageLink => await _messageProvider.SendLinkMessageAsync(request.SessionId, userId,
                request.LinkUrl!, request.LinkTitle, request.LinkDescription),
            MessageType.MessageExpression => await _messageProvider.SendExpressionMessageAsync(request.SessionId,
                userId, request.ExpressionCode!),
            _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
        };

        var dto = MapToDto(message);
        return Ok(ApiResponse<MessageDto>.Created(dto, "消息发送成功"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> GetMessage(Guid id)
    {
        var message = await _messageProvider.GetMessageAsync(id);
        if (message == null)
            return NotFound(ApiResponse<MessageDto>.NotFound("消息不存在"));

        return Ok(ApiResponse<MessageDto>.Ok(MapToDto(message)));
    }

    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetSessionMessages(
        Guid sessionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var messages = await _messageProvider.GetSessionMessagesAsync(sessionId, page, pageSize);
        var totalCount = await _messageProvider.GetMessageCountBySessionAsync(sessionId);

        var result = new PagedResult<MessageDto>
        {
            Items = messages.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> RecallMessage(Guid id,
        [FromQuery] RecallReason reason = RecallReason.UserRequest)
    {
        var userId = _currentUserService.GetUserId();
        await _messageProvider.RecallMessageAsync(id, userId, reason);
        return Ok(ApiResponse.Ok("消息已撤回"));
    }

    [HttpPost("{id}/forward")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> ForwardMessage(
        Guid id,
        [FromBody] ForwardMessageRequest request)
    {
        var userId = _currentUserService.GetUserId();
        var message = await _messageProvider.ForwardMessageAsync(id, request.TargetSessionId, userId,
            request.ForwardType, request.Comment);
        return Ok(ApiResponse<MessageDto>.Created(MapToDto(message), "消息转发成功"));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid id)
    {
        var userId = _currentUserService.GetUserId();
        await _messageProvider.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.Ok("已标记为已读"));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> SearchMessages(
        [FromQuery] Guid sessionId,
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var messages = await _messageProvider.SearchMessagesAsync(sessionId, searchTerm, page, pageSize);
        var result = new PagedResult<MessageDto>
        {
            Items = messages.Select(MapToDto).ToList(),
            TotalCount = messages.Count(),
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
    }

    [HttpGet("unread")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MessageDto>>>> GetUnreadMessages()
    {
        var userId = _currentUserService.GetUserId();
        var messages = await _messageProvider.GetUnreadMessagesAsync(userId);
        return Ok(ApiResponse<IEnumerable<MessageDto>>.Ok(messages.Select(MapToDto)));
    }

    private static MessageDto MapToDto(Domain.Entities.Message message) => new()
    {
        MessageId = message.MessageId,
        SessionId = message.SessionId,
        SenderId = message.SenderId,
        ReceiverId = message.ReceiverId,
        MessageType = message.MessageType,
        Status = message.Status,
        Content = message.Content,
        MediaUrl = message.MediaUri?.ToString(),
        ThumbnailUrl = message.ThumbnailUri,
        FileName = message.FileName,
        FileSize = (long?)message.FileSize,
        MimeType = message.MimeType,
        Duration = message.Duration,
        Caption = message.Caption,
        Latitude = message.Latitude,
        Longitude = message.Longitude,
        LocationName = message.LocationName,
        LinkUrl = message.LinkUrl,
        LinkTitle = message.LinkTitle,
        LinkDescription = message.LinkDescription,
        ExpressionCode = message.ExpressionCode,
        SentTime = message.SentTime,
        DeliveredTime = message.DeliveredTime,
        ReadTime = message.ReadTime,
        IsRecalled = message.IsRecalled,
        IsForwarded = message.IsForwarded,
        OriginalMessageId = message.OriginalMessageId,
        ReplyToMessageId = message.ReplyToMessageId
    };
}