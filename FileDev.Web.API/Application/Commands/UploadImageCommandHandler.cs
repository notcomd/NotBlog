using FileDev.Domain.Enum;
using ImageValidator = FileDev.Web.API.Grpc.ImageValidator;

namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 图片上传：格式验证 → 配额 → 写物理存储 → 创建元数据（失败补偿删除物理文件）。
/// 返回落库实体（含真实 FileId）与图片尺寸/格式。
/// </summary>
public class UploadImageCommandHandler(
    INotFileStorageService storageService,
    INotFileService notFileService,
    IContentAttachmentService contentAttachmentService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<UploadImageCommandHandler> logger)
    :  IRequestHandler<UploadImageCommand, UploadImageResult>
{
    public async Task<UploadImageResult> Handler(UploadImageCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        // Critical：FileName 非空校验前置，避免 Path.GetExtension 抛 NPE
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空");
        if (request.ImageContent == null || request.ImageContent.Length == 0)
            throw new ArgumentException("图片内容不能为空");

        // 图片格式验证（魔数 + 扩展名匹配，S-17 防伪装文件）
        if (request.ValidateFormat)
        {
            var formatResult = ImageValidator.Validate(request.ImageContent);
            if (!formatResult.IsValid)
                throw new ArgumentException($"图片格式验证失败: {formatResult.ErrorMessage}");

            var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
            if (!string.IsNullOrEmpty(ext))
            {
                var expectedExt = formatResult.Format switch
                {
                    "jpeg" => ".jpg",
                    _ => $".{formatResult.Format}"
                };
                if (ext != expectedExt && ext != $".{formatResult.Format}")
                    throw new ArgumentException($"图片实际格式({formatResult.Format})与扩展名({ext})不匹配");
            }
        }

        var options = configOptions.Value;

        // 统一前置校验（S-09/S-17：扩展名白名单、大小上限、用户配额）
        await notFileService.ValidateUploadAsync(
            request.UserId, request.FileName, request.ImageContent.Length, options, cancellationToken);

        var ext2 = Path.GetExtension(request.FileName).ToLowerInvariant();
        var relativePath = FileApiHelpers.BuildFileKey(request.UserId, ext2);
        var isAttachment = !string.IsNullOrWhiteSpace(request.ContentId);

        var storageResult = await storageService.SaveAsync(new NotFileStorageRequest
        {
            FileRelativePath = relativePath,
            FileContent = request.ImageContent,
            Overwrite = false,
            Source = isAttachment ? FileSource.ContentAttachment : FileSource.UserRepository
        }) ?? throw new InvalidOperationException("文件存储返回 null");

        if (!storageResult.Success)
            throw new InvalidOperationException($"文件存储失败: {storageResult.ErrorMessage}");

        var fileUri = FileApiHelpers.BuildFileUri(relativePath);

        NotFile file;
        try
        {
            file = await notFileService.CreateFileAsync(
                request.UserId, request.FileName, request.FileTags,
                request.FileDescription ?? string.Empty, Domain.Enum.FileType.FileImage,
                request.ImageContent.Length, fileUri,
                storageResult.ActualHash ?? string.Empty, request.FileIdentity,
                storageMeta: storageResult,
                source: isAttachment ? FileSource.ContentAttachment : FileSource.UserRepository);

            if (isAttachment)
            {
                await contentAttachmentService.RegisterAsync(
                    request.ContentId!, request.ContentType, fileUri, file.FileId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // 物理文件已写入但元数据保存失败时，补偿删除物理文件，避免存储泄漏。
            // 数据库事务由 TransactionBehavior 回滚。
            logger.LogWarning(ex, "[UploadImage] 元数据保存失败，清理物理文件: Path={Path}", relativePath);
            try
            {
                await storageService.DeleteAsync(relativePath);
            }
            catch (Exception cleanEx)
            {
                logger.LogError(cleanEx, "[UploadImage] 清理物理文件失败: Path={Path}", relativePath);
            }
            throw;
        }

        var (width, height, _) = ImageValidator.GetDimensions(request.ImageContent);
        return new UploadImageResult(file, width, height, ext2.TrimStart('.'));
    }
}
