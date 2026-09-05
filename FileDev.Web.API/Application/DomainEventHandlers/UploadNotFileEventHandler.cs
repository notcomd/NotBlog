namespace FileDev.Web.API.Application.DomainEventHandlers;
using FileDev.Domain;
using FileDev.Domain.Events;
using FileDev.Domain.IRepository;

public class UploadNotFileEventHandler(INotFileTagRepository notFileTagRepository,
    ILogger<UploadNotFileEventHandler> logger)
    : INotificationHandler<UploadNotFileEvent>
{
    private readonly INotFileTagRepository _notFileTagRepository =
        notFileTagRepository ?? throw new ArgumentNullException(nameof(notFileTagRepository));

    private readonly ILogger<UploadNotFileEventHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handler(UploadNotFileEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Domain] 文件上传事件已接收: FileId={FileId}, UserId={UserId}, FileName={FileName}",
            notification.FileId, notification.UserId, notification.FileName);

        
        var kind = FileTagDefaults.ResolveDefaultKind(notification.FileName);
        var targetTag = await _notFileTagRepository.GetNotFileTagByKindAsync(notification.UserId, kind);
        if (targetTag != null)
        {
            targetTag.AddFile(notification.FileId);
           
            await _notFileTagRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            _logger.LogInformation("[Domain] 文件已自动归类到默认标签: FileId={FileId}, Kind={Kind}, TagId={TagId}",
                notification.FileId, kind, targetTag.TagId);
        }
        else
        {
            _logger.LogWarning("[Domain] 未找到对应默认标签，跳过自动归类: UserId={UserId}, FileId={FileId}, Kind={Kind}",
                notification.UserId, notification.FileId, kind);
        }
    }
}