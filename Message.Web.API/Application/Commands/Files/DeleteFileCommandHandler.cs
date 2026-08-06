
namespace Message.Web.API.Application.Commands.Files;
/// <summary>
/// 删除文件命令处理程序。
/// <para>权限（修复 S-05）：仅上传者或所属会话成员可删除文件。</para>
/// <para>
/// 重新设计（MessageApi-Redesign v2）：软删附件记录后，级联调用 FileDev gRPC DeleteFile
/// 清理物理文件（FileDev 侧按 user_id 再次校验归属）。gRPC 删除失败不阻断本地软删
/// （附件记录是业务事实，物理文件由 FileDev 侧清理任务兜底），仅记录警告日志。
/// </para>
/// </summary>
public class DeleteFileCommandHandler(
    IFileAttachmentRepository fileRepository,
    IMessageRepository messageRepository,
    IChatSessionRepository sessionRepository,
    ICurrentUserService currentUser,
    IFileStorageGrpcClient fileStorage,
    IUnitOfWork unitOfWork,
    ILogger<DeleteFileCommandHandler> logger) : IRequestHandler<DeleteFileCommand, bool>
{
    public async Task<bool> Handler(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        var file = await fileRepository.GetByIdAsync(command.FileId);
        if (file == null)
            throw new KeyNotFoundException("文件不存在");

        var callerId = currentUser.GetUserId();
        if (!await FileAccessGuard.CanAccessAsync(file, messageRepository, sessionRepository, callerId))
            throw new UnauthorizedAccessException("无权删除该文件");

        file.Delete();
        await fileRepository.UpdateAsync(file);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        // 级联清理 FileDev 物理文件（尽力而为，失败仅告警）
        if (file.FileId != Guid.Empty)
        {
            var result = await fileStorage.DeleteFileAsync(file.FileId, callerId, cancellationToken);
            if (result is not { Success: true })
                logger.LogWarning("FileDev 物理文件删除失败：FileId={FileId}，原因={Reason}（附件记录已软删，等待清理任务兜底）",
                    file.FileId, result.ErrorMessage);
        }

        logger.LogInformation("文件 {FileId} 已删除", command.FileId);
        return true;
    }
}
