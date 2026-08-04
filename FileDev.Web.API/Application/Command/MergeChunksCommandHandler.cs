using FileDev.Domain.Entities;
using FileDev.Domain.IServices;
using FileDev.Web.API.APIs;

namespace FileDev.Web.API.Application.Command;

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
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new InvalidOperationException($"未找到上传任务: {request.FileKey}");

        // S-08：分片合并归属校验——用户 B 无法把用户 A 的分片合并到自己账号
        if (record.UserId != request.UserId)
            throw new UnauthorizedAccessException("无权合并此上传任务");

        // Major：合并前先校验分片完整性，避免在缺片情况下做无谓的配额检查
        if (!await chunkManager.AreAllChunksUploadedAsync(request.FileKey, cancellationToken))
            throw new InvalidOperationException($"分片未全部上传完毕: {request.FileKey}");

        // S-09：合并前配额检查（校验时机后移至完整性校验之后）
        var used = await notFileRepository.GetTotalFileSizeByUserIdAsync(request.UserId);
        if (used + record.TotalSize > configOptions.Value.UserStorageQuota)
            throw new InvalidOperationException("用户存储配额不足");

        // 合并分片
        var mergeResult = await storageService.MergeChunksAsync(
            request.FileKey, record.TotalChunks, null, true);

        if (!mergeResult.Success)
            throw new InvalidOperationException($"分片合并失败: {mergeResult.ErrorMessage}");

        // 标记完成
        await chunkManager.MarkMergedAsync(request.FileKey, cancellationToken);

        // 创建文件实体记录：fileKey 即 {userId:N}/{guid:N}{ext}，
        // 物理路径与下载 URI（/files/{fileKey}）一一对应；路径拼接统一收敛至 FileApiHelpers
        var fileUri = FileApiHelpers.BuildFileUri(request.FileKey);

        await notFileService.CreateFileAsync(
            record.UserId, record.FileName, record.FileTags,
            record.FileDescription ?? string.Empty, record.FileType,
            record.TotalSize, fileUri,
            mergeResult.ActualHash ?? record.FileMd5, record.FileIdentity);

        logger.LogInformation("[ChunkMerge] 文件合并完成: FileKey={FileKey}, FileName={FileName}",
            request.FileKey, record.FileName);

        // Major：返回的内存 NotFile 实体的 FileId 与持久化记录不同（NotFileId 在实体构造时新生成）。
        // 调用方（HTTP MergeChunksAsync）仅消费 FileName/FileUri/FileMd5，不需要真实 FileId；
        // gRPC MergeChunks 自行反查 savedFile.FileId 返回真实值。
        return new NotFile(
            record.UserId, record.FileName, record.FileTags,
            record.FileDescription ?? string.Empty,
            record.TotalSize, fileUri,
            mergeResult.ActualHash ?? record.FileMd5, record.FileIdentity);
    }
}
