namespace Markdown.Domain.Entities;

public enum MarkStatus
{
    /// <summary>
    ///  推送中
    /// </summary>
    MarkPushing,
    /// <summary>
    ///  推送成功
    /// </summary>
    MarkPushSuccess,
    /// <summary>
    ///  推送失败
    /// </summary>
    MarkPushFail,
    /// <summary>
    /// 推送取消
    /// </summary>
    MarkPushCancel,
    /// <summary>
    ///  等待推送
    /// </summary>
    MarkPushTimeout,
    /// <summary>
    ///  等待推送取消
    /// </summary>
    MarkPushError,
    /// <summary>
    ///  等待推送成功
    /// </summary>
    MarkPushWaiting,
    /// <summary>
    /// 等待推送取消成功
    /// </summary>
    MarkPushWaitingCancel,
    /// <summary>
    ///  等待推送取消超时
    /// </summary>
    MarkPushWaitingTimeout,
    /// <summary>
    ///  等待推送取消失败
    /// </summary>
    MarkPushWaitingError,
    /// <summary>
    ///  等待推送成功
    /// </summary>
    MarkPushWaitingSuccess,
    /// <summary>
    ///  等待推送失败
    /// </summary>
    MarkPushWaitingFail,
    /// <summary>
    /// 推送等待取消成功
    /// </summary>
    MarkPushWaitingCancelSuccess,
    /// <summary>
    /// 推送等待取消失败
    /// </summary>
    MarkPushWaitingCancelFail,
    /// <summary>
    /// 推送等待取消超时
    /// </summary>
    MarkPushWaitingCancelTimeout,
    /// <summary>
    ///  草稿（作者创建后的初始状态，对外不可见）
    /// </summary>
    MarkDraft,
    /// <summary>
    ///  待审核（已提交审核，对外不可见）
    /// </summary>
    MarkPendingReview,
    /// <summary>
    ///  审核通过（对外可见）
    /// </summary>
    MarkApproved,
    /// <summary>
    ///  审核驳回
    /// </summary>
    MarkRejected,
}
