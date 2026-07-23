namespace Message.Web.API.Dto;

public static class MessageMappingExtensions
{
    public static MessageDto MapToDto(this Domain.Entities.Message message) => new()
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
