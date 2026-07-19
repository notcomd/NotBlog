namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.Entities;
using FileDev.Domain.IServices;

public class StreamUploadCommandHandler(
    INotFileStorageService storageService,
    INotFileService notFileService,
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

        // 读取流内容
        using var ms = new MemoryStream();
        await request.FileStream.CopyToAsync(ms, cancellationToken);
        var content = ms.ToArray();

        // 生成存储路径
        var fileGuid = Guid.CreateVersion7();
        var relativePath = $"{request.UserId:N}/{fileGuid}{ext}";

        // 保存到文件系统
        var storageResult = await storageService.SaveAsync(new NotFileStorageRequest
        {
            FileRelativePath = relativePath,
            FileContent = content,
            Overwrite = false
        }) ?? throw new InvalidOperationException();

        if (!storageResult.Success)
            throw new InvalidOperationException($"文件存储失败: {storageResult.ErrorMessage}");

        var actualHash = storageResult.ActualHash ?? string.Empty;
        var fileUri = new Uri($"/files/{relativePath}", UriKind.Relative);

        // 创建数据库记录
        await notFileService.CreateFileAsync(
            request.UserId, request.FileName, request.FileTags,
            request.FileDescription ?? string.Empty, request.FileType,
            content.Length, fileUri, actualHash, request.FileIdentity);

        logger.LogInformation("[StreamUpload] 流式上传完成: {FileName}, Size={Size}",
            request.FileName, content.Length);

        return new NotFile(request.UserId, request.FileName, request.FileTags,
            request.FileDescription ?? string.Empty,
            content.Length, fileUri, actualHash, request.FileIdentity);
    }
}
