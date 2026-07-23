using NotMediator;

namespace Message.Web.API.Application.DomainEventHandlers;

public class FileUploadedEventHandler : INotificationHandler<FileUploadedEvent>
{
    private readonly ILogger<FileUploadedEventHandler> _logger;

    public FileUploadedEventHandler(ILogger<FileUploadedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handler(FileUploadedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("处理文件上传事件: AttachmentId={AttachmentId}", notification.AttachmentId);

        _logger.LogInformation("文件 {AttachmentId} '{FileName}' 已上传，大小: {FileSize} 字节",
            notification.AttachmentId, notification.FileName, notification.FileSize);

        await Task.CompletedTask;
    }
}