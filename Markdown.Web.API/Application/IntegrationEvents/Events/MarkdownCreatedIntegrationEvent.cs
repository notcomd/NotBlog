namespace Markdown.Web.API.Application.IntegrationEvents.Events;

/// <summary>
///     Markdown 文档创建集成事件（发布到 RabbitMQ）。
///     <para>本地由 MarkdownCreatedEventHandler 消费；Message 服务持有字段一致的事件副本（跨服务不共享程序集），
///     总线契约名 MarkdownCreated 必须保持一致。</para>
/// </summary>
[EventBusName("MarkdownCreated")]
public record MarkdownCreatedIntegrationEvent : IntegrationEvent
{
    /// <summary>文档标识</summary>
    public Guid MarkDownGuid { get; init; }

    /// <summary>文档作者</summary>
    public Guid MarkUserGuid { get; init; }

    /// <summary>文档名称</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; init; }
}