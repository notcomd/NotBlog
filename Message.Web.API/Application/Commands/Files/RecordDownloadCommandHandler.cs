namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 记录文件下载命令处理程序。
/// <para>先校验文件存在性，文件不存在则抛出 <see cref="KeyNotFoundException"/>。</para>
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可记录下载。</para>
/// </summary>
public class RecordDownloadCommandHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    ILogger<RecordDownloadCommandHandler> logger) : IRequestHandler<RecordDownloadCommand, bool>
{
    public async Task<bool> Handler(RecordDownloadCommand command, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(command.FileId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        var callerId = currentUser.GetUserId();
        if (!await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId))
            throw new UnauthorizedAccessException("无权下载该文件");

        file.RecordDownload();
        await fileRepository.UpdateAsync(file);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("文件 {FileId} 下载次数已记录", command.FileId);
        return true;
    }
}
