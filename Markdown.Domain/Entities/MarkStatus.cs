namespace Markdown.Domain.Entities;

/// <summary>
/// 文档审核状态机：草稿 -> 待审核 -> 通过 / 驳回（驳回后可重新提交）
/// 显式指定数值以保持与既有数据库记录兼容（旧版枚举前 15 个推送状态已移除，编号不重排）
/// </summary>
public enum MarkStatus
{
    /// <summary>
    ///  草稿（作者创建后的初始状态，对外不可见）
    /// </summary>
    MarkDraft = 15,

    /// <summary>
    ///  待审核（已提交审核，对外不可见）
    /// </summary>
    MarkPendingReview = 16,

    /// <summary>
    ///  审核通过（对外可见）
    /// </summary>
    MarkApproved = 17,

    /// <summary>
    ///  审核驳回
    /// </summary>
    MarkRejected = 18,
}
