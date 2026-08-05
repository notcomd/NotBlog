namespace FileDev.Web.API.Application.Queries;

/// <summary>
/// 获取单个文件信息。封装存在性校验与私有文件归属校验（S-08）。
/// </summary>
public class GetFileInfoQueryHandler(
    INotFileService notFileService)
    : NotMediator.IRequestHandler<GetFileInfoQuery, NotFile>
{
    public async Task<NotFile> Handler(GetFileInfoQuery request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var file = await notFileService.GetFileByIdAsync(request.FileId);
        if (file is null)
            throw new NotFileNotFoundException("文件不存在");

        // S-08：私有文件仅所有者可查看元数据
        if (file.FileIdentity == FileIdentity.FilePrivate && file.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权访问此文件");

        return file;
    }
}
