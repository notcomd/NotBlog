namespace FileDev.Web.API.Application.DomainEventHandlers;
using NotMediator;
using FileDev.Domain.Events;
public class UploadNotFileEventHandler(INotFileService notFileService, 
ILogger<UploadNotFileEventHandler> logger)
    : INotificationHandler<UploadNotFileEvent>
{
    private readonly ILogger<UploadNotFileEventHandler> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    public async Task Handler(UploadNotFileEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Domain] 文件上传事件已接收: FileId={FileId}, UserId={UserId}, FileName={FileName}",
            notification.NotFile.Id, notification.UserGuid, notification.FileName);
        await Task.CompletedTask;
    }
    
}