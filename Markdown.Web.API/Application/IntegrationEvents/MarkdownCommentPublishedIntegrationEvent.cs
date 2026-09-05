namespace Markdown.Web.API.Application.IntegrationEvents;

/// <summary>
///     Markdown 评论发布集成事件（顶级评论通知博客作者；子评论通知被回复的评论作者）。
///     <para>消费端：Message 服务 MarkdownCommentPublished 消费者 Handler。</para>
/// </summary>
[EventBusName("MarkdownCommentPublished")]
public record MarkdownCommentPublishedIntegrationEvent(
    Guid MarkDownGuid,
    string MarkDownName,
    /// <summary>新评论标识（前端跳转用）</summary>
    Guid ReviewGuid,
    /// <summary>子评论（回复）才有；顶级评论为 null</summary>
    Guid? ParentReviewGuid,
    /// <summary>截断后的评论预览</summary>
    string CommentContent,
    /// <summary>评论者</summary>
    Guid ActorUserId,
    /// <summary>通知对象：顶级评论=博客作者；子评论=父评论作者</summary>
    Guid TargetUserId,
    DateTimeOffset OccurredAt) : IntegrationEvent;