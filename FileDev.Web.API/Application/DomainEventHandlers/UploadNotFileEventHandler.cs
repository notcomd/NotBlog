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
        _logger.LogInformation("[Domain] 文件上传事件已接收: NotFileId={NotFileId}, UserId={UserId}, FileName={FileName}",
            notification.NotFileId, notification.UserGuid, notification.FileName);

        // 自动关联到用户根组
        var rootGroups = await _notFileGroupRepository.GetRootGroupsByUserIdAsync(notification.UserGuid);
        var rootGroup = rootGroups.FirstOrDefault();
        if (rootGroup != null)
        {
            rootGroup.AddFile(notification.NotFileId);
            _logger.LogInformation("[Domain] 文件已关联到根文件组: NotFileId={NotFileId}, GroupId={GroupId}",
                notification.NotFileId, rootGroup.NotFileGroupId);
        }
        else
        {
            _logger.LogWarning("[Domain] 用户无根文件组，跳过文件组关联: UserId={UserId}, NotFileId={NotFileId}",
                notification.UserGuid, notification.NotFileId);
        }
    }
}
