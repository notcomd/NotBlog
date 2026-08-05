namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 删除文件。调用领域服务执行软删除并触发 DeleteFileEvent（物理文件清理），
/// 事务与领域事件分发由 TransactionBehavior 统一处理。
/// </summary>
public class DeleteFileCommandHandler(
    INotFileService notFileService,
    ILogger<DeleteFileCommandHandler> logger)
    : NotMediator.IRequestHandler<DeleteFileCommand, bool>
{
    public async Task<bool> Handler(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        await notFileService.DeleteFileAsync(request.FileId, request.UserId);
        logger.LogInformation("[DeleteFile] 文件已软删除: FileId={FileId}", request.FileId);
        return true;
    }
}
