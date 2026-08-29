namespace Markdown.Domain.IRepository;

/// <summary>
///     交互操作结果（Count + 是否首次发生）。
///     <para>
///     首次发生（IsFirst = true）表示本请求真正产生了新的交互记录（点赞/投币/评论点赞/评论踩），
///     只有此时才应发布作者通知事件；重复/幂等分支（IsFirst = false）不得重复通知。
///     </para>
/// </summary>
public readonly record struct InteractionResult(long Count, bool IsFirst);