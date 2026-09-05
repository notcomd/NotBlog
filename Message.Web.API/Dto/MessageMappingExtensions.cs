using Message.Domain.ValueObjects.Message;

namespace Message.Web.API.Dto;

public static class MessageMappingExtensions
{
    public static MessageDto MapToDto(this Domain.Entities.Chat.Message message) => new()
    {
        MessageId = message.MessageId,
        SessionId = message.SessionId,
        SenderId = message.SenderId,
        ReceiverId = message.ReceiverId,
        MessageType = message.MessageType,
        Status = message.Status,
        // 内容已收敛为多态 MessageContent，按具体子类摊平到 DTO 字段（保持对外契约不变）
        Content = message.Content is TextContent text ? text.Value : null,
        MediaUrl = message.Content is MediaContent media ? media.MediaUri.ToString() : null,
        ThumbnailUrl = message.Content is MediaContent mediaThumb ? mediaThumb.ThumbnailUri : null,
        FileName = message.Content is FileContent fileN ? fileN.FileName : null,
        FileSize = message.Content is FileContent fileS ? fileS.FileSize : null,
        MimeType = message.Content is FileContent fileM ? fileM.MimeType : null,
        Duration = message.Content is MediaContent mediaDur ? mediaDur.Duration : null,
        Caption = message.Content is MediaContent mediaCap ? mediaCap.Caption : null,
        Latitude = message.Content is LocationContent locLat ? locLat.Latitude : null,
        Longitude = message.Content is LocationContent locLng ? locLng.Longitude : null,
        LocationName = message.Content is LocationContent locN ? locN.LocationName : null,
        LinkUrl = message.Content is LinkContent linkUrl ? linkUrl.Url.ToString() : null,
        LinkTitle = message.Content is LinkContent linkT ? linkT.Title : null,
        LinkDescription = message.Content is LinkContent linkD ? linkD.Description : null,
        ExpressionCode = message.Content is ExpressionContent expr ? expr.Value : null,
        SentTime = message.SentTime,
        DeliveredTime = message.DeliveredTime,
        ReadTime = message.ReadTime,
        IsRecalled = message.IsRecalled,
        IsForwarded = message.IsForwarded,
        OriginalMessageId = message.OriginalMessageId,
        ReplyToMessageId = message.ReplyToMessageId
    };
}
