using Notcomd.Token.JWT.Security;

namespace FileDev.Web.API.Application.Commands;

/// <summary>
/// 小文件上传：校验 → 配额 → 写物理存储 → 创建元数据（失败补偿删除物理文件）。
/// 返回落库实体（含真实 FileId）。
/// </summary>
public class UploadFileCommandHandler(
    INotFileStorageService storageService,
    INotFileService notFileService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<UploadFileCommandHandler> logger)
    : NotMediator.IRequestHandler<UploadFileCommand, NotFile>
{
    public async Task<NotFile> Handler(UploadFileCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        // Critical：FileName 非空校验前置，避免 Path.GetExtension 抛 NPE
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空");
        if (request.FileContent == null || request.FileContent.Length == 0)
            throw new ArgumentException("文件内容不能为空");

        var options = configOptions.Value;

        // 统一前置校验（S-09/S-17：扩展名白名单、大小上限、用户配额）
        await notFileService.ValidateUploadAsync(
            request.UserId, request.FileName, request.FileContent.Length, options, cancellationToken);

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();

        // SHA256 校验（F-09.5：上传/查重统一使用 SHA256）
        if (!string.IsNullOrWhiteSpace(request.ExpectedMd5))
        {
            var actualHash = HashHelper.ComputeHash(request.FileContent);
            if (!string.Equals(actualHash, request.ExpectedMd5, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"哈希校验失败，预期: {request.ExpectedMd5}，实际: {actualHash}");
        }

        // 物理存储写入
        var relativePath = FileApiHelpers.BuildFileKey(request.UserId, ext);
        var storageResult = await storageService.SaveAsync(new NotFileStorageRequest
        {
            FileRelativePath = relativePath,
            FileContent = request.FileContent,
            Overwrite = false,
            ExpectedHash = request.ExpectedMd5
        }) ?? throw new InvalidOperationException("文件存储返回 null");

        if (!storageResult.Success)
            throw new InvalidOperationException($"文件存储失败: {storageResult.ErrorMessage}");

        var fileUri = FileApiHelpers.BuildFileUri(relativePath);
        var fileType = FileApiHelpers.ResolveFileType(ext);

        try
        {
            return await notFileService.CreateFileAsync(
                request.UserId, request.FileName, request.FileTags,
                request.FileDescription ?? string.Empty, fileType,
                request.FileContent.Length, fileUri,
                storageResult.ActualHash ?? string.Empty, request.FileIdentity);
        }
        catch (Exception ex)
        {
            // 物理文件已写入但元数据保存失败时，补偿删除物理文件，避免存储泄漏。
            // 数据库事务由 TransactionBehavior 回滚。
            logger.LogWarning(ex, "[UploadFile] 元数据保存失败，清理物理文件: Path={Path}", relativePath);
            try
            {
                await storageService.DeleteAsync(relativePath);
            }
            catch (Exception cleanEx)
            {
                logger.LogError(cleanEx, "[UploadFile] 清理物理文件失败: Path={Path}", relativePath);
            }
            throw;
        }
    }
}
