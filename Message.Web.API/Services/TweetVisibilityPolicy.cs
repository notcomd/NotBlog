namespace Message.Web.API.Services;

/// <summary>
/// Tweet 可见性判定策略（S-17 可见性过滤；R-03 接入关注关系）。
/// <para>
/// 规则：
/// - 作者本人对本人推文始终可见（含 Private / Followers / 草稿）；
/// - <see cref="Visibility.Public"/> 对所有可见；
/// - <see cref="Visibility.Followers"/> 仅作者的关注者可见（关注关系见 <see cref="IUserFollowRepository"/>，
///   调用方负责一次性查询查看者的关注集合传入，避免逐条 N+1）；
/// - <see cref="Visibility.Private"/> 仅作者可见。
/// </para>
/// </summary>
public static class TweetVisibilityPolicy
{
    /// <summary>
    /// 判断推文对指定查看者是否可见。
    /// </summary>
    /// <param name="tweet">推文实体</param>
    /// <param name="viewerId">查看者用户 ID（未认证时为 <see cref="Guid.Empty"/>）</param>
    /// <param name="followingIds">查看者的关注集合（仅 <see cref="Visibility.Followers"/> 判定使用；可空）</param>
    /// <returns>true 表示可见</returns>
    public static bool IsVisibleTo(Tweet tweet, Guid viewerId, IReadOnlySet<Guid>? followingIds = null)
    {
        if (tweet.AuthorGuid == viewerId)
            return true;

        if (tweet.Visibility == Visibility.Public)
            return true;

        // R-03：Followers 语义 = 仅作者的关注者可见；关注关系已实现（UserFollow），不再退化为仅作者可见
        if (tweet.Visibility == Visibility.Followers)
            return followingIds?.Contains(tweet.AuthorGuid) == true;

        // Private 仅作者可见
        return false;
    }
}
