using FileDev.Domain.Entities;
using FileDev.Domain.Enum;
using FileDev.Domain.IServices;
using FileDev.Web.API.APIs;

namespace FileDev.Web.API.Application.Commands;

public class MergeChunksCommandHandler(
    INotFileStorageService storageService,
    IFileChunkManager chunkManager,
    INotFileService notFileService,
    IContentAttachmentService contentAttachmentService,
    IOptionsSnapshot<NotFileStorageOptions> configOptions,
    ILogger<MergeChunksCommandHandler> logger)
    :  IRequestHandler<MergeChunksCommand, NotFile>
{
    public async Task<NotFile> Handler(MergeChunksCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FileKey))
            throw new ArgumentException("FileKey不能为空");
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");

        var record = await chunkManager.GetUploadStatusAsync(request.FileKey, cancellationToken);
        if (record == null)
            throw new NotFileNotFoundException($"未找到上传任务: {request.FileKey}");

        // S-08：分片合并归属校验——用户 B 无法把用户 A 的分片合并到自己账号
        if (record.UserId != request.UserId)
            throw new FilePermissionDeniedException("无权合并此上传任务");

        // Major：合并前先校验分片完整性，避免在缺片情况下做无谓的配额检查
        if (!await chunkManager.AreAllChunksUploadedAsync(request.FileKey, cancellationToken))
            throw new InvalidOperationException($"分片未全部上传完毕: {request.FileKey}");

        // S-09：合并前配额检查（统一走 UserFileInfo 记账表校验，且置于分片完整性校验之后）
        await notFileService.ValidateUploadAsync(
            request.UserId, request.FileName ?? record.FileName, record.TotalSize,
            configOptions.Value, cancellationToken);

        // 合并分片（产物按归属类别路由到对应物理池）
        var mergeSource = string.IsNullOrWhiteSpace(request.ContentId)
            ? FileSource.UserRepository
            : FileSource.ContentAttachment;
        var mergeResult = await storageService.MergeChunksAsync(
            request.FileKey, record.TotalChunks, null, true,
            new StoreContext(mergeSource, record.UserId.ToString("N")));

        if (!mergeResult.Success)
            throw new InvalidOperationException($"分片合并失败: {mergeResult.ErrorMessage}");

        // 标记完成
        await chunkManager.MarkMergedAsync(request.FileKey, cancellationToken);

        // 创建文件实体记录：fileKey 即 {userId:N}/{guid:N}{ext}，
        // 物理路径与下载 URI（/files/{fileKey}）一一对应；路径拼接统一收敛至 FileApiHelpers。
        // 元数据优先使用调用方传入的覆盖值，未提供时回退到分片记录值（与 HTTP 合并链路行为一致）
        var fileUri = FileApiHelpers.BuildFileUri(request.FileKey);

        var isAttachment = !string.IsNullOrWhiteSpace(request.ContentId);

        var file = await notFileService.CreateFileAsync(
            record.UserId,
            request.FileName ?? record.FileName,
            request.FileTags ?? record.FileTags,
            request.FileDescription ?? record.FileDescription ?? string.Empty,
            record.FileType,
            record.TotalSize, fileUri,
            mergeResult.ActualHash ?? record.FileMd5, record.FileIdentity,
            storageMeta: mergeResult,
            source: isAttachment ? FileSource.ContentAttachment : FileSource.UserRepository);

        if (isAttachment)
        {
            await contentAttachmentService.RegisterAsync(
                request.ContentId!, request.ContentType, fileUri, file.FileId, cancellationToken);
        }

        // 合并成功记账（配额不足抛异常，由事务回滚文件记录）
        await notFileService.OccupyQuotaAsync(request.UserId, record.TotalSize, cancellationToken);

        logger.LogInformation("[ChunkMerge] 文件合并完成: FileKey={FileKey}, FileName={FileName}",
            request.FileKey, record.FileName);

        // 返回落库实体（FileId 在实体构造时生成），gRPC 可直接回显真实 FileId，无需再反查
        return file;
    }
}
