namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.Entities;
using FileDev.Domain.IServices;

public class MergeChunksCommandHandler(
    INotFileStorageService storageService,
    IFileChunkManager chunkManager,
    INotFileService notFileService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<MergeChunksCommandHandler> logger)
    : NotMediator.IRequestHandler<MergeChunksCommand, NotFile>
{
    public async Task<NotFile> Handler(MergeChunksCommand request, CancellationToken cancellationToken)
    {
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");

        if (!await chunkManager.AreAllChunksUploadedAsync(request.FileKey, cancellationToken))
            throw new InvalidOperationException($"分片未全部上传完毕: {request.FileKey}");

        // 合并分片
        var ext = Path.GetExtension(record.FileName).ToLowerInvariant();
        var mergeResult = await storageService.MergeChunksAsync(
            request.FileKey, record.TotalChunks, null, true);

        if (!mergeResult.Success)
            throw new InvalidOperationException($"分片合并失败: {mergeResult.ErrorMessage}");

        // 标记完成
        await chunkManager.MarkMergedAsync(request.FileKey, cancellationToken);

        // 创建文件实体记录
        var fileGuid = Guid.CreateVersion7();
        var relativePath = $"{record.UserId:N}/{fileGuid}{ext}";
        var fileUri = new Uri($"/files/{relativePath}", UriKind.Relative);

        await notFileService.CreateFileAsync(
            record.UserId, record.FileName, record.FileTags,
            record.FileDescription ?? string.Empty, record.FileType,
            record.TotalSize, fileUri,
            mergeResult.ActualHash ?? record.FileMd5, record.FileIdentity);

        logger.LogInformation("[ChunkMerge] 文件合并完成: FileKey={FileKey}, FileName={FileName}",
            request.FileKey, record.FileName);

        // 返回 NotFile (需要通过 repository 查询)
        // 通过 notFileService 创建后无法直接返回 entity,
        // 返回一个简化的 NotFile 标记
        return new NotFile(
            record.UserId, record.FileName, record.FileTags,
            record.FileDescription ?? string.Empty,
            record.TotalSize, fileUri,
            mergeResult.ActualHash ?? record.FileMd5, record.FileIdentity);
    }
}
