namespace Markdown.Domain.Entities;

public enum MarkReviewType
{
    /// <summary>
    /// 公开评论
    /// </summary>
    ReviewAuthPublic,
    /// <summary>
    /// 私有评论
    /// </summary>
    ReviewAuthPrivate,
    /// <summary>
    /// 受保护评论
    /// </summary>
    ReviewAuthProtected,

    /// <summary>
    /// 仅作者可见
    /// </summary>
    ReviewAuthOnlyAuthorVisible,

    /// <summary>
    /// 仅作者和评论者可见
    /// </summary>
    ReviewAuthOnlyAuthorAndReviewerVisible,

    /// <summary>
    /// 评论消息
    /// </summary>
    ReviewMessage,

    /// <summary>
    /// 评论置顶
    /// </summary>
    ReviewTop

}