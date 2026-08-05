using ImageValidator = FileDev.Web.API.Grpc.ImageValidator;

namespace FileDev.Web.API.Application.Queries;

/// <summary>
/// 获取图片信息：校验存在性与归属（S-08），读取物理内容解析尺寸（小图片）。
/// </summary>
public class GetImageInfoQueryHandler(
    INotFileService notFileService,
    INotFileStorageService storageService)
    : NotMediator.IRequestHandler<GetImageInfoQuery, ImageInfoResult>
{
    public async Task<ImageInfoResult> Handler(GetImageInfoQuery request, CancellationToken cancellationToken)
    {
        if (request.FileId == Guid.Empty)
            throw new ArgumentException("文件ID不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var file = await notFileService.GetFileByIdAsync(request.FileId);
        if (file is null)
            throw new NotFileNotFoundException("文件不存在");

        // S-08：私有图片仅所有者可查看
        if (file.FileIdentity == FileIdentity.FilePrivate && file.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权访问此文件");

        int width = 0, height = 0;
        string format = "unknown";

        // S-09：GetContentAsync 会整读文件入内存，仅用于小图片尺寸解析；
        // 超过 100MB（与 Kestrel 请求体上限一致）的文件跳过尺寸解析，避免大文件整读入内存
        if (file.FileSize <= 100L * 1024 * 1024)
        {
            var relativePath = FileApiHelpers.FileUriToRelativePath(file.FileUri);
            var (content, storageResponse) = await storageService.GetContentAsync(relativePath);
            if (storageResponse.Success && content is not null)
            {
                (width, height, _) = ImageValidator.GetDimensions(content);
                format = Path.GetExtension(file.FileName).ToLowerInvariant().TrimStart('.');
            }
        }

        return new ImageInfoResult(file, width, height, format);
    }
}
