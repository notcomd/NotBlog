using FileDev.Domain.Events;
using FileDev.Domain.IRepository;

namespace FileDev.Web.API.Application.DomainEventHandlers;


public class FileDeleteEventHandler(INotFileRepository fileRepository
,ILogger<FileDeleteEventHandler> logger) :INotificationHandler<FileDeletedEvent>
{
    private readonly INotFileRepository _fileRepository = fileRepository;
    private readonly ILogger<FileDeleteEventHandler> _logger = logger;

    public async Task Handler(FileDeletedEvent notification, CancellationToken cancellationToken)
    {
        // 处理文件删除事件
    }
}