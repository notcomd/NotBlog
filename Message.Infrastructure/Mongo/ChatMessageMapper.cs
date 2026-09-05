using Message.Domain.Entities.Chat;
using Message.Domain.ValueObjects.Message;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Infrastructure.Mongo;

/// <summary>
/// <see cref="MessageEntity"/>（领域聚合）与 <see cref="ChatMessageDocument"/>（Mongo 投影）互转。
/// 写路径由实体投影为文档；读路径由文档重建实体（经 <see cref="MessageEntity.Rebuild"/>）。
/// <para>
/// 领域内容已收敛为多态 <see cref="MessageContent"/>，此处负责将该值对象摊平到文档列
/// （与既有库表/Mongo 文档结构保持一致，存量数据不破坏），反向则按 <see cref="MessageType"/> 重建内容值对象。
/// </para>
/// </summary>
public static class ChatMessageMapper
{
    /// <summary>实体 → 文档</summary>
    public static ChatMessageDocument ToDocument(MessageEntity message)
    {
        var content = message.Content;
        return new ChatMessageDocument
        {
            MessageId = message.MessageId,
            SessionId = message.SessionId,
            SenderId = message.SenderId,
            ReceiverId = message.ReceiverId,
            MessageType = message.MessageType,
            Status = message.Status,
            Content = content is TextContent text ? text.Value : null,
            MediaUri = content is MediaContent media ? media.MediaUri.ToString() : null,
            ThumbnailUri = content is MediaContent mediaThumb ? mediaThumb.ThumbnailUri : null,
            FileSize = content is FileContent file ? file.FileSize : null,
            Duration = content is MediaContent mediaDur ? mediaDur.Duration : null,
            FileName = content is FileContent fileN ? fileN.FileName : null,
            MimeType = content is FileContent fileM ? fileM.MimeType : null,
            Caption = content is MediaContent mediaCap ? mediaCap.Caption : null,
            Latitude = content is LocationContent loc ? loc.Latitude : null,
            Longitude = content is LocationContent locLng ? locLng.Longitude : null,
            LocationName = content is LocationContent locN ? locN.LocationName : null,
            LinkUrl = content is LinkContent link ? link.Url.ToString() : null,
            LinkTitle = content is LinkContent linkT ? linkT.Title : null,
            LinkDescription = content is LinkContent linkD ? linkD.Description : null,
            ExpressionCode = content is ExpressionContent expr ? expr.Value : null,
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
            ToContent(doc), doc.Status,
            doc.SentTime, doc.DeliveredTime, doc.ReadTime,
            doc.IsRecalled, doc.IsEncrypted, doc.IsForwarded,
            doc.OriginalMessageId, doc.ReplyToMessageId,
            doc.Attachments.Select(ToAttachmentEntity).ToList());
    }

    /// <summary>按文档消息类型重建多态内容值对象。</summary>
    private static MessageContent ToContent(ChatMessageDocument doc) => doc.MessageType switch
    {
        MessageType.MessageText => TextContent.Create(doc.Content ?? string.Empty),
        MessageType.MessageImage => ImageContent.Create(ToUriOrThrow(doc.MediaUri, nameof(doc.MediaUri)),
            doc.ThumbnailUri, doc.Caption),
        MessageType.MessageVideo => VideoContent.Create(ToUriOrThrow(doc.MediaUri, nameof(doc.MediaUri)),
            doc.Duration ?? 0, doc.ThumbnailUri, doc.Caption),
        MessageType.MessageAudio => AudioContent.Create(ToUriOrThrow(doc.MediaUri, nameof(doc.MediaUri)),
            doc.Duration ?? 0, doc.Caption),
        MessageType.MessageFile => FileContent.Create(ToUriOrThrow(doc.MediaUri, nameof(doc.MediaUri)),
            doc.FileName ?? string.Empty, doc.FileSize ?? 0, doc.MimeType ?? string.Empty),
        MessageType.MessageLocation => LocationContent.Create(doc.Latitude ?? 0, doc.Longitude ?? 0,
            doc.LocationName ?? string.Empty),
        MessageType.MessageLink => LinkContent.Create(ToUriOrThrow(doc.LinkUrl, nameof(doc.LinkUrl)),
            doc.LinkTitle, doc.LinkDescription),
        MessageType.MessageExpression => ExpressionContent.Create(doc.ExpressionCode ?? string.Empty),
        _ => throw new NotSupportedException($"不支持的消息类型: {doc.MessageType}")
    };

    private static Uri ToUriOrThrow(string? value, string paramName)
        => value is not null ? new Uri(value) : throw new InvalidOperationException($"{paramName} 缺失");

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