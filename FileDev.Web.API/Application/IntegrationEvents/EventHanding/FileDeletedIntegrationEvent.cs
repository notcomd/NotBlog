using Notcomd.EventBus.Core;
using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

[EventBusName("File.Deleted")]
public class FileDeletedIntegrationEventHandler(
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
