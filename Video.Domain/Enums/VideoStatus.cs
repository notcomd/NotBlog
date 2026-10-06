namespace Video.Domain.Enums;

/// <summary>
/// 视频内容审核状态 — 描述视频从草稿到发布的生命周期（与 <c>Message.Domain.Enums.TweetStatus</c> 语义对齐）。
/// <para>
/// 与权限枚举 <see cref="AuthorVideo"/> 相互独立：<see cref="AuthorVideo"/> 决定「谁能看」，
/// 本枚举决定「是否已通过审核、可被公开展示」。两者通过实体 <c>Videos</c> 维护
/// 「<see cref="Approved"/> ⟺ VideoControl.VideoDisplay == true」的不变量。
/// </para>
/// </summary>
public enum VideoStatus
{
    /// <summary>
    /// 草稿状态，未提交审核。
    /// </summary>
    Draft,

    /// <summary>
    /// 待审核状态，已提交审核，等待管理员审核。
    /// </summary>
    Pending,

    /// <summary>
    /// 已通过状态，管理员已审核通过。
    /// </summary>
    Approved,

    /// <summary>
    /// 已拒绝状态，管理员已审核拒绝。
    /// </summary>
    Rejected
}
