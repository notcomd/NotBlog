using Notcomd.Evenbus;

namespace Markdown.Web.API.Application.IntegrationEventHandlers;

/// <summary>
///     Markdown 文档创建事件处理器（集成事件）
/// </summary>
[EvenBusName("MarkdownCreated")]
public class MarkdownCreatedEventHandler : JsonIntegrationEventHandler<MarkdownCreatedEventData>
{
    protected override Task EventDlerJson(string eventName, MarkdownCreatedEventData? eventData)
    {
        if (eventData == null)
            return Task.CompletedTask;

        // TODO: 处理 Markdown 文档创建后的业务逻辑
        // 例如：发送通知、更新索引、触发工作流等

        Console.WriteLine($"收到 Markdown 创建事件：{eventData.MarkDownGuid}, 文件名：{eventData.FileName}");

        return Task.CompletedTask;
    }
}

/// <summary>
///     Markdown 文档创建事件数据
/// </summary>
public record MarkdownCreatedEventData
{
    public Guid MarkDownGuid { get; init; }
    public Guid MarkUserGuid { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}