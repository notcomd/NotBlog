namespace FileDev.Web.API.Application.Queries;

/// <summary>
/// 下载文件：校验存在性与归属（S-08），流式读取物理内容（S-09），
/// 返回实体元数据与内容流供上层流式响应。
/// </summary>
public class DownloadFileQueryHandler(
    INotFileService notFileService,
    INotFileStorageService storageService)
    : NotMediator.IRequestHandler<DownloadFileQuery, FileDownloadResult>
{
    public async Task<FileDownloadResult> Handler(DownloadFileQuery request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var file = await notFileService.GetFileByIdAsync(request.FileId);
        if (file is null)
            throw new NotFileNotFoundException("文件不存在");

        // S-08：私有文件仅所有者可下载
        if (file.FileIdentity == FileIdentity.FilePrivate && file.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权访问此文件");

        var relativePath = FileApiHelpers.FileUriToRelativePath(file.FileUri);
        var (stream, storageResponse) = await storageService.GetContentStreamAsync(relativePath);
        if (!storageResponse.Success || stream is null)
            throw new InvalidOperationException($"存储读取错误: {storageResponse.ErrorMessage}");

        return new FileDownloadResult(file, stream);
    }
}
