namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.Entities;
using FileDev.Domain.IServices;

public class MergeChunksCommandHandler(
    INotFileStorageService storageService,
    IFileChunkManager chunkManager,
    INotFileService notFileService,
    FileDev.Domain.IRepository.INotFileRepository notFileRepository,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<MergeChunksCommandHandler> logger)
    : NotMediator.IRequestHandler<MergeChunksCommand, NotFile>
{
    public async Task<NotFile> Handler(MergeChunksCommand request, CancellationToken cancellationToken)
    {
        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");

        // S-08：分片合并归属校验——用户 B 无法把用户 A 的分片合并到自己账号
        if (record.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权合并此上传任务");

        // S-09：合并前配额检查
        var used = await notFileRepository.GetTotalFileSizeByUserIdAsync(request.UserId);
        if (used + record.TotalSize > configOptions.Value.UserStorageQuota)
            throw new InvalidOperationException("用户存储配额不足");

        if (!await chunkManager.AreAllChunksUploadedAsync(request.FileKey, cancellationToken))
            throw new InvalidOperationException($"分片未全部上传完毕: {request.FileKey}");

        // 合并分片
        var mergeResult = await storageService.MergeChunksAsync(
            request.FileKey, record.TotalChunks, null, true);

        if (!mergeResult.Success)
            throw new InvalidOperationException($"分片合并失败: {mergeResult.ErrorMessage}");

        // 标记完成
        await chunkManager.MarkMergedAsync(request.FileKey, cancellationToken);

        // 创建文件实体记录：fileKey 即 {userId:N}/{guid:N}{ext}，
        // 物理路径与下载 URI（/files/{fileKey}）一一对应，上传后可按 URI 下载
        var relativePath = request.FileKey;
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
