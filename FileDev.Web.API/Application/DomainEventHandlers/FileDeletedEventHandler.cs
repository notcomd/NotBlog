using FileDev.Domain.Events;
using FileDev.Domain.IServices;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 文件软删除事件处理（F-09.4）：软删除后清理物理文件。
/// 秒传（F-09.1）可能使多个归属记录共享同一物理文件（同一 FileUri），
/// 因此仅当不存在其他活跃记录引用该物理文件时才执行物理删除。
/// </summary>
public class FileDeleteEventHandler(
    NotFileDbContext dbContext,
    INotFileStorageService storageService,
    ILogger<FileDeleteEventHandler> logger) : INotificationHandler<DeleteFileEvent>
{
    public async Task Handler(DeleteFileEvent notification, CancellationToken cancellationToken)
    {
        var file = await dbContext.NotFiles
            .FirstOrDefaultAsync(f => f.FileId == notification.FileId, cancellationToken);
        if (file is null)
        {
            logger.LogWarning("[FileDelete] 未找到文件记录: FileId={FileId}", notification.FileId);
            return;
        }

        // 检查是否还有其他活跃记录共享同一物理文件（秒传复用场景）
        var otherUris = await dbContext.NotFiles
            .Where(f => f.FileId != file.FileId && !f.IsDeleted)
            .Select(f => f.FileUri)
            .ToListAsync(cancellationToken);
        var otherActiveRefs = otherUris.Count(uri => uri == file.FileUri);

        if (otherActiveRefs > 0)
        {
            logger.LogInformation(
                "[FileDelete] 物理文件仍被其他记录引用，跳过物理删除: FileId={FileId}, FileUri={FileUri}",
                notification.FileId, file.FileUri);
            return;
        }

        // FileUri 形如 /files/{userId}/{guid}{ext}，去掉 /files/ 前缀即为存储相对路径
        var relativePath = file.FileUri.ToString();
        if (relativePath.StartsWith("/files/", StringComparison.Ordinal))
            relativePath = relativePath["/files/".Length..];

        var result = await storageService.DeleteAsync(relativePath);
        if (result.Success)
        {
            logger.LogInformation("[FileDelete] 物理文件已删除: FileId={FileId}, Path={Path}",
                notification.FileId, relativePath);
        }
        else
        {
            logger.LogWarning("[FileDelete] 物理文件删除失败: FileId={FileId}, Path={Path}, Error={Error}",
                notification.FileId, relativePath, result.ErrorMessage);
        }
    }
}
