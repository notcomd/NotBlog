using Notcomd.EventBus.Core;
using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 文件创建集成事件处理器：仅记录日志（接收方按需扩展业务逻辑）。
/// </summary>
[EventBusName("FileCreatedIntegrationEvent")]
public class FileCreatedIntegrationEventHandler(ILogger<FileCreatedIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<FileCreatedEventData>
{
    public override async Task Handler(FileCreatedEventData eventData)
    {
        logger.LogInformation("[FileCreatedIntegrationEventHandler] 文件创建事件已接收: FileName={FileName}, UserId={UserId}",
            eventData.FileName, eventData.UserId);
        await Task.CompletedTask;
    }
}
