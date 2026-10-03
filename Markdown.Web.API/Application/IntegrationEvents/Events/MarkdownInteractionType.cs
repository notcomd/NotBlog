namespace Markdown.Web.API.Application.IntegrationEvents.Events;

/// <summary>
///     Markdown 交互类型（点赞/投币/评论点赞/评论踩）。
///     仅"首次发生"的交互才发布对应通知事件（重复互动幂等分支不发）
/// </summary>
public enum MarkdownInteractionType
{
    DocumentLiked,
    DocumentCoined,
    ReviewLiked,
    ReviewDisliked
}