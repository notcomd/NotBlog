namespace Markdown.Web.API.Application.IntegrationEvents;

/// <summary>
///     Markdown 交互集成事件（点赞/投币/评论点赞/评论踩；仅首次发生发布）。
///     <para>消费端：Message 服务 MarkdownInteraction 消费者 Handler，用于生成作者站内通知。</para>
/// </summary>
[EventBusName("MarkdownInteraction")]
public record MarkdownInteractionIntegrationEvent(
    MarkdownInteractionType InteractionType,
    Guid MarkDownGuid,
    string MarkDownName,
    /// <summary>评论类交互定位到具体评论（前端跳转用）；文档类交互为 null</summary>
    Guid? ReviewGuid,
    /// <summary>操作者</summary>
    Guid ActorUserId,
    /// <summary>通知对象：文档交互取文档作者；评论交互取评论作者</summary>
    Guid TargetUserId,
    /// <summary>投币数量（仅 DocumentCoined 有效，其余为 0）</summary>
    long Amount,
    DateTimeOffset OccurredAt) : IntegrationEvent;