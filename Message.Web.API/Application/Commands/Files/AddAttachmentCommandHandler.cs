
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 给已发送消息补附件命令处理程序。
/// </summary>
public class AddAttachmentCommandHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    IFileStorageGrpcClient fileStorage,
    IUnitOfWork unitOfWork,
    ILogger<AddAttachmentCommandHandler> logger) : IRequestHandler<AddAttachmentCommand, Guid>
{
    public async Task<Guid> Handler(AddAttachmentCommand command, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(command.MessageId);
        if (message == null)
            throw new KeyNotFoundException("消息不存在");

        var session = await sessionRepository.GetByIdAsync(message.SessionId);
        if (session == null || !session.IsParticipant(command.CallerId))
            throw new UnauthorizedAccessException("无权操作该消息");

        // 与 SendMessageCommand 一致：FileDev 校验文件存在性 + 归属
        var info = await fileStorage.GetFileInfoAsync(command.FileId, cancellationToken);
        if (!info.Success || info.FileId == null)
            throw new KeyNotFoundException(info.ErrorMessage ?? "文件不存在");
        if (info.UserId != command.CallerId)
            throw new UnauthorizedAccessException("无权使用该文件");

        var fileUri = info.FileUri ?? throw new InvalidOperationException("文件URI缺失");
        var mimeType = MimeTypeMap.FromFileName(info.FileName);

        var attachment = new FileAttachment(
            command.MessageId, command.FileId, info.FileName, mimeType, info.FileSize, fileUri, mimeType);
        message.AddAttachment(attachment);
        await fileRepository.AddAsync(attachment);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("消息 {MessageId} 补附件成功：FileId={FileId}，附件={AttachmentId}",
            command.MessageId, command.FileId, attachment.AttachmentId);
        return attachment.AttachmentId;
    }
}
