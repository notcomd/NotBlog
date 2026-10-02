using FileDev.Domain.Events;
using FileDev.Domain.IRepository;
using FileDev.Domain.IServices;

namespace FileDev.Web.API.Application.DomainEventHandlers;

/// <summary>
/// 文件软删除事件处理（F-09.4）：软删除后清理物理文件。
/// 秒传（F-09.1）可能使多个归属记录共享同一物理文件（同一 FileUri），
/// 因此仅当不存在其他活跃记录引用该物理文件时才执行物理删除。
/// 方案 C：内容附件（ContentRef）仍引用该 URI 时同样跳过物理删除，
/// 物理回收改由 UnregisterContentAttachments 在引用清零后统一触发。
/// </summary>
public class FileDeleteEventHandler(
    INotFileRepository notFileRepository,
    IContentAttachmentRefRepository contentAttachmentRefRepository,
    INotFileStorageService storageService,
    ILogger<FileDeleteEventHandler> logger) : INotificationHandler<DeleteFileEvent>
{
    public async Task Handler(DeleteFileEvent notification, CancellationToken cancellationToken)
    {
        var file = await notFileRepository.GetFileByIdAsync(notification.FileId);
        if (file is null)
        {
            logger.LogWarning("[FileDelete] 未找到文件记录: FileId={FileId}", notification.FileId);
            return;
        }

        // 检查是否还有其他活跃记录共享同一物理文件（秒传复用场景），数据库端 Count 统计避免全表加载
        var otherActiveRefs = await notFileRepository.CountActiveRefsByFileUriAsync(file.FileUri, file.FileId);

        // 方案 C：内容附件弱引用仍在时，物理文件交由附件回收流程管理
        var activeContentRefs = await contentAttachmentRefRepository.CountActiveByFileUriAsync(
            file.FileUri, cancellationToken);

        if (otherActiveRefs > 0 || activeContentRefs > 0)
        {
            logger.LogInformation(
                "[FileDelete] 物理文件仍被其他记录/内容附件引用，跳过物理删除: FileId={FileId}, FileUri={FileUri}",
                notification.FileId, file.FileUri);
            return;
        }

        // FileUri 形如 /files/{userId}/{guid}{ext}，去掉 /files/ 前缀即为存储相对路径
        // Major：路径还原统一收敛至 FileApiHelpers.FileUriToRelativePath
        var relativePath = FileApiHelpers.FileUriToRelativePath(file.FileUri);

        // 命名空间以文件属主为准；领域事件可能脱离请求上下文，不能依赖当前用户的环境租户
        var result = await storageService.DeleteAsync(relativePath,
            cancellationToken, new StoreContext(file.Source, file.UserId.ToString("N")));
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