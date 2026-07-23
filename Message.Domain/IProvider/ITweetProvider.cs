using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;

namespace Message.Domain.IProvider;

public interface ITweetProvider
{
    /// <summary>
    /// 创建推文
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="content">推文内容</param>
    /// <param name="mediaUrls">媒体URL列表</param>
    /// <param name="linkUrl">链接URL</param>
    /// <param name="hashtags">标签列表</param>
    /// <param name="visibility">可见性（字符串）</param>
    /// <returns>创建后的推文详情</returns>
    Task<Tweet> CreateTweetAsync(Guid authorGuid, string content, IEnumerable<string>? mediaUrls = null,
        string? linkUrl = null, IEnumerable<string>? hashtags = null,
        string? visibility = null);

    /// <summary>
    /// 保存草稿
    /// </summary>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="content">推文内容</param>
    /// <param name="mediaUrls">媒体URL列表</param>
    /// <param name="linkUrl">链接URL</param>
    /// <param name="hashtags">标签列表</param>
    /// <param name="visibility">可见性（字符串）</param>
    /// <returns>保存后的草稿详情</returns>
    Task<Tweet> SaveDraftAsync(Guid authorGuid, string content, IEnumerable<string>? mediaUrls = null,
        string? linkUrl = null, IEnumerable<string>? hashtags = null,
        string? visibility = null);

    /// <summary>
    /// 发布草稿
    /// </summary>
    /// <param name="tweetGuid">推稿ID</param>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>发布后的推稿详情</returns>
    Task<Tweet> PublishDraftAsync(Guid tweetGuid, Guid authorGuid);

    /// <summary>
    /// 获取推文详情
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <returns>推文详情</returns>
    Task<Tweet?> GetTweetAsync(Guid tweetGuid);

    /// <summary>
    /// 获取用户推文列表
    /// </summary>
    /// <param name="userGuid">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>用户推文列表</returns>
    Task<IEnumerable<Tweet>> GetUserTweetsAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取用户时间线推文列表
    /// </summary>
    /// <param name="userGuid">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>用户时间线推文列表</returns>
    Task<IEnumerable<Tweet>> GetTimelineAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>
    /// 获取趋势推文列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>趋势推文列表</returns>
    Task<IEnumerable<Tweet>> GetTrendingAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// 更新草稿
    /// </summary>
    /// <param name="tweetGuid">推稿ID</param>
    /// <param name="authorGuid">作者ID</param>
    /// <param name="content">推文内容</param>
    /// <param name="mediaUrls">媒体URL列表</param>
    /// <param name="linkUrl">链接URL</param>
    /// <param name="hashtags">标签列表</param>
    /// <param name="visibility">可见性（字符串）</param>
    /// <returns>更新后的草稿详情</returns>
    Task<Tweet> UpdateDraftAsync(Guid tweetGuid, Guid authorGuid, string content,
        IEnumerable<string>? mediaUrls = null, string? linkUrl = null,
        IEnumerable<string>? hashtags = null, string? visibility = null);

    /// <summary>
    /// 删除推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>删除后的推文详情</returns>
    Task DeleteTweetAsync(Guid tweetGuid, Guid authorGuid);

    /// <summary>
    /// 置顶推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>置顶后的推文详情</returns>
    Task PinTweetAsync(Guid tweetGuid, Guid authorGuid);

    /// <summary>
    /// 取消置顶推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="authorGuid">作者ID</param>
    /// <returns>取消置顶后的推文详情</returns>
    Task UnpinTweetAsync(Guid tweetGuid, Guid authorGuid);

    /// <summary>
    /// 点赞推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>点赞后的推文详情</returns>
    Task<Tweet> LikeAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 取消点赞推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>取消点赞后的推文详情</returns>
    Task<Tweet> UnlikeAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 收藏推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>收藏后的推文详情</returns>
    Task<Tweet> FavoriteAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 取消收藏推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>取消收藏后的推文详情</returns>
    Task<Tweet> UnfavoriteAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 分享推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>分享后的推文详情</returns>
    Task<Tweet> ShareAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 投币推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <returns>投币后的推文详情</returns>
    Task<Tweet> CoinAsync(Guid tweetGuid, Guid userGuid);

    /// <summary>
    /// 记录推文查看次数
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userGuid">用户ID</param>
    /// <param name="viewerIp">查看IP</param>
    Task RecordViewAsync(Guid tweetGuid, Guid? userGuid, string? viewerIp);

    /// <summary>
    /// 获取用户与推文的交互状态
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="type">交互类型</param>
    /// <returns>是否存在该交互</returns>
    Task<bool> GetInteractionStatusAsync(Guid tweetGuid, Guid userId, InteractionType type);
}
