using FileDev.Domain.Entities;
using FileDev.Web.API.APIs;

namespace FileDev.Web.API.Application.Command;

public class StreamUploadCommandHandler(
    INotFileStorageService storageService,
    INotMediator mediator,
    FileDev.Domain.IRepository.INotFileRepository notFileRepository,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<StreamUploadCommandHandler> logger)
    : NotMediator.IRequestHandler<StreamUploadCommand, NotFile>
{
    public async Task<NotFile> Handler(StreamUploadCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空");

        var options = configOptions.Value;
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        logger.LogDebug("[StreamUpload] 文件名校验: FileName={FileName}, Ext={Ext}, WhitelistCount={Count}",
            request.FileName, ext, options.AllowedExtensions.Count);

        // Major：白名单为空时跳过校验，与 ChunkUploadInitCommandHandler 保持一致
        if (options.AllowedExtensions is { Count: > 0 })
        {
            if (!options.AllowedExtensions.Contains(ext))
            {
                logger.LogWarning("[StreamUpload] 拒绝 — 扩展名不在白名单: FileName={FileName}, Ext={Ext}",
                    request.FileName, ext);
                throw new ArgumentException($"不支持的文件类型: {ext}");
            }

            logger.LogInformation("[StreamUpload] 白名单校验通过: FileName={FileName}, Ext={Ext}",
                request.FileName, ext);
        }
        else
        {
            logger.LogDebug("[StreamUpload] 白名单为空，跳过扩展名校验: FileName={FileName}", request.FileName);
        }

        if (request.FileSize > options.MaxFileSize)
            throw new ArgumentException($"文件大小超过限制 {options.MaxFileSize / 1024 / 1024}MB");

        // S-09：写入前配额检查
        var used = await notFileRepository.GetTotalFileSizeByUserIdAsync(request.UserId);
        if (used + request.FileSize > options.UserStorageQuota)
            throw new InvalidOperationException("用户存储配额不足");

        // 读取流内容（#17：API 层已整读请求体为 MemoryStream 传入，这里直接取数，避免重复整读）
        byte[] content;
        if (request.FileStream is MemoryStream memStream)
        {
            memStream.Position = 0;
            content = memStream.ToArray();
        }
        else
        {
            using var ms = new MemoryStream();
            await request.FileStream.CopyToAsync(ms, cancellationToken);
            content = ms.ToArray();
        }

        // Major：统一路径拼接（原散落在 Handler 内的字符串插值）
        var relativePath = FileApiHelpers.BuildFileKey(request.UserId, ext);

        // 保存到文件系统
        var storageResult = await storageService.SaveAsync(new NotFileStorageRequest
        {
            FileRelativePath = relativePath,
            FileContent = content,
            Overwrite = false
        }) ?? throw new InvalidOperationException("文件存储返回 null");

        if (!storageResult.Success)
            throw new InvalidOperationException($"文件存储失败: {storageResult.ErrorMessage}");

        var actualHash = storageResult.ActualHash ?? string.Empty;
        var fileUri = FileApiHelpers.BuildFileUri(relativePath);

        logger.LogInformation("[StreamUpload] 流式上传完成: FileName={FileName}, Size={Size}",
            request.FileName, content.Length);

        // Major：原代码使用 CreateNotFileCommand + 显式属性赋值存在重复，
        // 统一通过构造函数传参（与记录主体字段一致），避免冗余赋值和 ! null 抑制
        var uploadCmd = new CreateNotFileCommand(
            request.UserId,
            request.FileName,
            fileUri,
            actualHash,
            request.FileIdentity,
            content.Length,
            request.FileTags,
            request.FileDescription
        );

        try
        {
            await mediator.SendAsync(uploadCmd, cancellationToken);
        }
        catch (Exception ex)
        {
            // Major：补偿回滚 — 物理文件已写入但数据库元数据保存失败时，
            // 删除遗留的物理文件，避免存储泄漏。事务由 TransactionBehavior 回滚。
            logger.LogWarning(ex, "[StreamUpload] 元数据保存失败，清理物理文件: Path={Path}", relativePath);
            try
            {
                await storageService.DeleteAsync(relativePath);
            }
            catch (Exception cleanEx)
            {
                logger.LogError(cleanEx, "[StreamUpload] 清理物理文件失败: Path={Path}", relativePath);
            }
            throw;
        }

        // 返回内存构建的 NotFile 实体（FileId 与持久化记录不同，
        // TransactionBehavior 提交后由 UploadNotFileEventHandler 自动关联根组；
        // 调用方仅需 FileName/FileUri/FileMd5 等元数据，无需 FileId）
        return new NotFile.NotFileBuilder()
            .WithUserId(request.UserId)
            .WithFileName(request.FileName)
            .WithFileTags(request.FileTags ?? [])
            .WithFileDescription(request.FileDescription ?? string.Empty)
            .WithFileSize(content.Length)
            .WithFileUri(fileUri)
            .WithFileMd5(actualHash)
            .WithFileIdentity(request.FileIdentity)
            .Build();
    }
}
