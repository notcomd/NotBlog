namespace Message.Domain.Enums;
public enum TweetStatus
{
    /// <summary>
    /// 推文草稿状态，未提交审核。
    /// </summary>
    Draft,
    /// <summary>
    /// 推文待审核状态，已提交审核，等待管理员审核。
    /// </summary>
    Pending,
    /// <summary>
    /// 推文已审核状态，管理员已审核通过。
    /// </summary>
    Approved,
    
    /// <summary>
    /// 推文已拒绝状态，管理员已审核拒绝。
    /// </summary>
    Rejected
}

