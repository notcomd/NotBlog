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
public class MessagesController(IMessageProvider messageProvider, ICurrentUserService currentUserService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageRequest request)
    {
        var userId = currentUserService.GetUserId();
        var message = request.MessageType switch
        {
            MessageType.MessageText => await messageProvider.SendTextMessageAsync(request.SessionId, userId,
                request.Content ?? ""),
            MessageType.MessageImage => await messageProvider.SendImageMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Caption, request.ThumbnailUrl),
            MessageType.MessageVideo => await messageProvider.SendVideoMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption, request.ThumbnailUrl),
            MessageType.MessageAudio => await messageProvider.SendAudioMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.Duration ?? 0, request.Caption),
            MessageType.MessageFile => await messageProvider.SendFileMessageAsync(request.SessionId, userId,
                new Uri(request.MediaUrl!), request.FileName!, request.FileSize ?? 0, request.MimeType!),
            MessageType.MessageLocation => await messageProvider.SendLocationMessageAsync(request.SessionId, userId,
                request.Latitude ?? 0, request.Longitude ?? 0, request.LocationName!),
            MessageType.MessageLink => await messageProvider.SendLinkMessageAsync(request.SessionId, userId,
                request.LinkUrl!, request.LinkTitle, request.LinkDescription),
            MessageType.MessageExpression => await messageProvider.SendExpressionMessageAsync(request.SessionId, userId,
                request.ExpressionCode!),
            _ => throw new NotSupportedException($"不支持的消息类型: {request.MessageType}")
        };

        var dto = MapToDto(message);
        return Ok(ApiResponse<MessageDto>.Created(dto, "消息发送成功"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> GetMessage(Guid id)
    {
        var message = await messageProvider.GetMessageAsync(id);
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
        var messages = await messageProvider.GetSessionMessagesAsync(sessionId, page, pageSize);
        var totalCount = await messageProvider.GetMessageCountBySessionAsync(sessionId);

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
        var userId = currentUserService.GetUserId();
        await messageProvider.RecallMessageAsync(id, userId, reason);
        return Ok(ApiResponse.Ok("消息已撤回"));
    }

    [HttpPost("{id}/forward")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> ForwardMessage(
        Guid id,
        [FromBody] ForwardMessageRequest request)
    {
        var userId = currentUserService.GetUserId();
        var message = await messageProvider.ForwardMessageAsync(id, request.TargetSessionId, userId,
            request.ForwardType, request.Comment);
        return Ok(ApiResponse<MessageDto>.Created(MapToDto(message), "消息转发成功"));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid id)
    {
        var userId = currentUserService.GetUserId();
        await messageProvider.MarkAsReadAsync(id, userId);
        return Ok(ApiResponse.Ok("已标记为已读"));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> SearchMessages(
        [FromQuery] Guid sessionId,
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var messages = await messageProvider.SearchMessagesAsync(sessionId, searchTerm, page, pageSize);
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
        var userId = currentUserService.GetUserId();
        var messages = await messageProvider.GetUnreadMessagesAsync(userId);
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