namespace FileDev.Web.API.Application.Commands;

using FileDev.Domain.IRepository;

public class DeduplicateFileCommandHandler(
    INotFileRepository notFileRepository,
    ILogger<DeduplicateFileCommandHandler> logger)
    : NotMediator.IRequestHandler<DeduplicateFileCommand, DeduplicateFileResponse>
{
    public async Task<DeduplicateFileResponse> Handler(DeduplicateFileCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空");
        if (string.IsNullOrWhiteSpace(request.FileMd5))
            throw new ArgumentException("文件哈希不能为空");
        if (request.FileSize < 0)
            throw new ArgumentOutOfRangeException(nameof(request.FileSize), "文件大小不能为负数");

        // F-09.1：秒传命中时优先返回调用者自己的记录，避免重复创建；
        // 查询条件同时匹配 MD5 与文件大小（MD5 相同但大小不同不应误命中秒传）
        var existing = await notFileRepository.GetDeduplicateFileAsync(
            request.FileMd5, request.FileSize, request.UserId);

        if (existing == null)
        {
            logger.LogDebug("[Dedup] 未命中秒传: Md5={Md5}", request.FileMd5);
            return new DeduplicateFileResponse { Exists = false };
        }

        // 调用者已拥有该文件 → 直接返回
        if (existing.UserId == request.UserId)
        {
            logger.LogInformation("[Dedup] 秒传命中（本人文件）: Md5={Md5}, FileId={FileId}",
                request.FileMd5, existing.FileId);
            return new DeduplicateFileResponse
            {
                Exists = true,
                FileId = existing.FileId,
                FileUri = existing.FileUri.ToString()
            };
        }

        // 文件属于其他用户 → 为当前用户复用物理文件并建立归属记录
        // （共享同一 FileUri，物理文件仅存一份；软删除时仅当最后一个活跃记录被删才清理物理文件）
        var shared = new NotFile.NotFileBuilder()
            .WithUserId(request.UserId)
            .WithFileName(existing.FileName)
            .WithFileTags(existing.FileTags)
            .WithFileDescription(existing.FileDescription)
            .WithFileSize(existing.FileSize)
            .WithFileUri(existing.FileUri)
            .WithFileMd5(existing.FileMd5)
            .WithFileIdentity(existing.FileIdentity)
            .Build();
        await notFileRepository.InsertFileAsync(shared);
        await notFileRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        logger.LogInformation("[Dedup] 秒传命中（复用物理文件并绑定用户）: Md5={Md5}, FileId={FileId}",
            request.FileMd5, shared.FileId);
        return new DeduplicateFileResponse
        {
            Exists = true,
            FileId = shared.FileId,
            FileUri = shared.FileUri.ToString()
        };
    }
}