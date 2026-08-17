namespace FileDev.Web.API.Application.DomainEventHandlers;
using FileDev.Domain.Events;
using FileDev.Domain.IRepository;

public class UploadNotFileEventHandler(INotFileGroupRepository notFileGroupRepository,
    ILogger<UploadNotFileEventHandler> logger)
    : INotificationHandler<UploadNotFileEvent>
{
    private readonly INotFileGroupRepository _notFileGroupRepository =
        notFileGroupRepository ?? throw new ArgumentNullException(nameof(notFileGroupRepository));

    private readonly ILogger<UploadNotFileEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handler(UploadNotFileEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Domain] 文件上传事件已接收: FileId={FileId}, UserId={UserId}, FileName={FileName}",
            notification.FileId, notification.UserId, notification.FileName);

        // 自动关联到用户根组
        var rootGroups = await _notFileGroupRepository.GetRootGroupsByUserIdAsync(notification.UserId);
        var rootGroup = rootGroups.FirstOrDefault();
        if (rootGroup != null)
        {
            rootGroup.AddFile(notification.FileId);
            // 修复（2026-08-15）：NotMediator 的事件分发为异步后台（Channel 消费），且 handler
            // 由根容器解析（与请求事务的 DbContext 非同一实例）——此处对根组的修改不在请求
            // 事务内，必须显式保存，否则"文件关联根组"的副作用会丢失。
            await _notFileGroupRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            _logger.LogInformation("[Domain] 文件已关联到根文件组: FileId={FileId}, GroupId={GroupId}",
                notification.FileId, rootGroup.NotFileGroupId);
        }
        else
        {
            _logger.LogWarning("[Domain] 用户无根文件组，跳过文件组关联: UserId={UserId}, FileId={FileId}",
                notification.UserId, notification.FileId);
        }
    }
}
