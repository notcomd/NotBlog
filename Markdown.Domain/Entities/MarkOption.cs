namespace Markdown.Domain.Entities;

/// <summary>
/// markdown操作权限
/// </summary>
public enum MarkOption
{
    Default,

    Allow,

    ForbidEdit,

    Forbid,

    AllowReview,

    ForbidReview,
}