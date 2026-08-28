using Message.Domain.Entities.Chat;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// <see cref="MessageEntity"/>（领域聚合）与 <see cref="ChatMessageDocument"/>（Mongo 投影）互转。
/// 写路径由实体投影为文档；读路径由文档重建实体（经 <see cref="MessageEntity.Rebuild"/>）。
/// </summary>
public static class ChatMessageMapper
{
    /// <summary>实体 → 文档</summary>
    public static ChatMessageDocument ToDocument(MessageEntity message)
    {
        return new ChatMessageDocument
        {
            MessageId = message.MessageId,
            SessionId = message.SessionId,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            MessageType = message.MessageType,
            Status = message.Status,
            Content = message.Content,
            MediaUri = message.MediaUri?.ToString(),
            ThumbnailUri = message.ThumbnailUri,
            FileSize = message.FileSize,
            Duration = message.Duration,
            FileName = message.FileName,
            MimeType = message.MimeType,
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
            IsEncrypted = message.IsEncrypted,
            IsForwarded = message.IsForwarded,
            OriginalMessageId = message.OriginalMessageId,
            ReplyToMessageId = message.ReplyToMessageId,
            Attachments = message.Attachments.Select(ToAttachmentDocument).ToList()
        };
    }

    /// <summary>文档 → 实体</summary>
    public static MessageEntity ToEntity(ChatMessageDocument doc)
    {
        return MessageEntity.Rebuild(
            doc.MessageId, doc.SessionId, doc.SenderId, doc.ReceiverId,
            doc.MessageType, doc.Status,
            doc.Content,
            doc.MediaUri is null ? null : new Uri(doc.MediaUri),
            doc.ThumbnailUri, doc.FileSize, doc.Duration,
            doc.FileName, doc.MimeType, doc.Caption,
            doc.Latitude, doc.Longitude, doc.LocationName,
            doc.LinkUrl, doc.LinkTitle, doc.LinkDescription, doc.ExpressionCode,
            doc.SentTime, doc.DeliveredTime, doc.ReadTime,
            doc.IsRecalled, doc.IsEncrypted, doc.IsForwarded,
            doc.OriginalMessageId, doc.ReplyToMessageId,
            doc.Attachments.Select(ToAttachmentEntity).ToList());
    }

    private static ChatFileAttachmentDocument ToAttachmentDocument(FileAttachment attachment)
    {
        return new ChatFileAttachmentDocument
        {
            AttachmentId = attachment.AttachmentId,
            MessageId = attachment.MessageId,
            FileId = attachment.FileId,
            FileName = attachment.FileName,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
            FileUri = attachment.FileUri.ToString(),
            ThumbnailUri = attachment.ThumbnailUri?.ToString(),
            MimeType = attachment.MimeType,
            Description = attachment.Description,
            UploadTime = attachment.UploadTime,
            DownloadTime = attachment.DownloadTime,
            DownloadCount = attachment.DownloadCount,
            IsDeleted = attachment.IsDeleted
        };
    }

    private static FileAttachment ToAttachmentEntity(ChatFileAttachmentDocument doc)
    {
        return FileAttachment.Rebuild(
            doc.AttachmentId, doc.MessageId, doc.FileId,
            doc.FileName, doc.FileType, doc.FileSize,
            new Uri(doc.FileUri),
            doc.ThumbnailUri is null ? null : new Uri(doc.ThumbnailUri),
            doc.MimeType, doc.Description,
            doc.UploadTime, doc.DownloadTime, doc.DownloadCount, doc.IsDeleted);
    }
}