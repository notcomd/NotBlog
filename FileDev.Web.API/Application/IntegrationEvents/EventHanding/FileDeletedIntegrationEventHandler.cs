using Notcomd.EventBus.Core;
using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 文件删除集成事件处理器：仅消费记录，不再原样 re-publish
/// （原样转发无目的地区分，可能造成事件风暴/循环转发）。
/// </summary>
[EventBusName("File.Deleted")]
public class FileDeletedIntegrationEventHandler(
 ILogger<FileDeletedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<FileDeletedEventData>
{
    public override Task Handler(FileDeletedEventData eventData)
    {
        logger.LogInformation("[Integration] 文件删除事件已接收: FileId={FileId}, UserId={UserId}",
            eventData.FileId, eventData.UserId);
        return Task.CompletedTask;
    }
}
