using Notcomd.EventBus.Core;
using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

[EventBusName("FileCreatedIntegrationEvent")]
public class FileCreatedIntegrationEventHandler(INotFileService notFileService, ILogger<FileCreatedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<FileCreatedEventData>
{
    public override async Task Handler(FileCreatedEventData eventData)
    {
        logger.LogInformation("[FileCreatedIntegrationEventHandler] 文件创建事件已接收: FileName={FileName}, UserId={UserId}",
            eventData.FileName, eventData.UserId);
        await Task.CompletedTask;
    }
}

public record FileCreatedEventData(Guid FileId, Guid UserId, string FileName, long FileSize, string FileType) : IntegrationEvent;
