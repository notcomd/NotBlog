namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 更新文件元数据。封装存在性校验与归属校验（S-08），
/// 调用领域服务执行更新，并返回更新后的落库实体。
/// </summary>
public class UpdateFileInfoCommandHandler(
    INotFileService notFileService)
    : NotMediator.IRequestHandler<UpdateFileInfoCommand, NotFile>
{
    public async Task<NotFile> Handler(UpdateFileInfoCommand request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空");

        // S-08：仅文件所有者可更新元数据
        var existing = await notFileService.GetFileByIdAsync(request.FileId);
        if (existing is null)
            throw new NotFileNotFoundException("文件不存在");
        if (existing.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权修改此文件");

        await notFileService.UpdateFileAsync(
            request.FileId, request.FileName, request.FileTags,
            request.FileDescription, request.FileIdentity, request.FileMd5);

        // 同一 DbContext 作用域内返回更新后的跟踪实体
        var updated = await notFileService.GetFileByIdAsync(request.FileId);
        if (updated is null)
            throw new NotFileNotFoundException("文件不存在");
        return updated;
    }
}
