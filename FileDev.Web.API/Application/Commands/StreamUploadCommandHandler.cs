using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Web.API.APIs;

namespace FileDev.Web.API.Application.Commands;

public class StreamUploadCommandHandler(
    INotFileStorageService storageService,
    INotMediator mediator,
    INotFileService notFileService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<StreamUploadCommandHandler> logger)
    :  IRequestHandler<StreamUploadCommand, NotFile>
{
    public async Task<NotFile> Handler(StreamUploadCommand request, CancellationToken cancellationToken)
    {
        var options = configOptions.Value;

        // 统一前置校验（S-09/S-17：扩展名白名单、大小上限、用户配额）
        await notFileService.ValidateUploadAsync(
            request.UserId, request.FileName, request.FileSize, options, cancellationToken);

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();

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

        // 保存到文件系统（流式上传为用户个人文件，写 user-repo 池）
        var storageResult = await storageService.SaveAsync(new NotFileStorageRequest
        {
            FileRelativePath = relativePath,
            FileContent = content,
            Overwrite = false,
            Source = FileSource.UserRepository
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
        )
        {
            StorageMeta = storageResult
        };

        try
        {
            await mediator.SendAsync(uploadCmd, cancellationToken);

            // 上传成功记账（配额不足抛异常，由事务回滚文件记录）
            await notFileService.OccupyQuotaAsync(request.UserId, content.Length, cancellationToken);
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

