using Evenbus.Core;
using Microsoft.Extensions.Logging;
using Notcomd.Evenbus;

namespace FileDev.Web.API.Application.IntegrationEvents;

[EvenBusName("File.Deleted")]
public class FileDeletedIntegrationEventHandler(INotFileService notFileService,
 ILogger<FileDeletedIntegrationEventHandler> logger,
 IEventBus eventBus)
    : JsonIntegrationEventHandler<FileDeletedEventData>
{
    public override async Task Handler(FileDeletedEventData eventData)
    {
        logger.LogInformation("[Integration] 文件删除事件已接收: FileId={FileId}, UserId={UserId}",
            eventData.FileId, eventData.UserId);
        await eventBus.PublishAsync(eventData);
        await Task.CompletedTask;
    }
}

public record FileDeletedEventData(Guid FileId, Guid UserId, string FileName) : IntegrationEvent;
