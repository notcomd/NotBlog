namespace Markdown.Domain.Entities;

/// <summary>
/// markdown操作权限
/// </summary>
public enum MarkOption
{
    /// <summary>
    ///  默认
    /// </summary>
    Default,

    /// <summary>
    /// 允许
    /// </summary>
    Allow,

    /// <summary>
    /// 禁止编辑
    /// </summary>
    ForbidEdit,

    /// <summary>
    /// 禁止
    /// </summary>
    Forbid,

    /// <summary>
    /// 允许审核
    /// </summary>
    AllowReview,

    /// <summary>
    /// 禁止审核
    /// </summary>
    ForbidReview,
}