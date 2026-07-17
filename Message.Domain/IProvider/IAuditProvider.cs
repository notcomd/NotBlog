using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;

namespace Message.Domain.IProvider;

public interface IAuditProvider
{
    /// <summary>
    /// 获取待处理的推文列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>待处理的推文列表</returns>
    Task<IEnumerable<Tweet>> GetPendingTweetsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 审核通过推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="auditorGuid">审核人ID</param>
    Task ApproveTweetAsync(Guid tweetGuid, Guid auditorGuid);

    /// <summary>
    /// 审核拒绝推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="auditorGuid">审核人ID</param>
    /// <param name="reason">拒绝原因</param>
    Task RejectTweetAsync(Guid tweetGuid, Guid auditorGuid, string reason);

    /// <summary>
    /// 获取待处理的举报列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>待处理的举报列表</returns>
    Task<IEnumerable<TweetReport>> GetPendingReportsAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 审核通过举报
    /// </summary>
    /// <param name="reportGuid">举报ID</param>
    /// <param name="reviewerGuid">审核人ID</param>
    /// <param name="note">审核备注</param>
    /// <param name="isContentRemoved">是否删除内容</param>
    Task ResolveReportAsync(Guid reportGuid, Guid reviewerGuid, string note, bool isContentRemoved);


    //Task<IEnumerable<TweetReport>> GetPendingReportsAsync();
}
