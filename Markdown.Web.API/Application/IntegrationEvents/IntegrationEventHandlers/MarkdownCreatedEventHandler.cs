namespace Markdown.Web.API.Application.IntegrationEvents.IntegrationEventHandlers;

/// <summary>
///     Markdown 文档创建事件处理器（集成事件）
/// </summary>
[EventBusName("MarkdownCreated")]
public class MarkdownCreatedEventHandler(ILogger<MarkdownCreatedEventHandler> logger)
    : JsonIntegrationEventHandler<MarkdownCreatedEventData>
{
    public override Task Handler(MarkdownCreatedEventData eventData)
    {
        // TODO: 处理 Markdown 文档创建后的业务逻辑
        // 例如：发送通知、更新索引、触发工作流等

        logger.LogInformation("收到 Markdown 创建事件：{MarkDownGuid}, 文件名：{FileName}",
            eventData.MarkDownGuid, eventData.FileName);

        return Task.CompletedTask;
    }
}

/// <summary>
///     Markdown 文档创建事件数据
/// </summary>
public record MarkdownCreatedEventData : IntegrationEvent
{
    public Guid MarkDownGuid { get; init; }
    public Guid MarkUserGuid { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
