namespace Message.Web.API.Services;

/// <summary>
/// Tweet 可见性判定策略（S-17 可见性过滤）。
/// <para>
/// 规则：
/// - 作者本人对本人推文始终可见（含 Private / Followers / 草稿）；
/// - <see cref="Visibility.Public"/> 对所有可见；
/// - <see cref="Visibility.Private"/> 仅作者可见；
/// - <see cref="Visibility.Followers"/> 应仅作者的关注者可看；当前 Message 模块未实现关注关系
///   （仅有好友关系 MessageFriends），故退化为"仅作者可见"，后续接入关注关系后在此扩展。
/// </para>
/// </summary>
public static class TweetVisibilityPolicy
{
    /// <summary>
    /// 判断推文对指定查看者是否可见。
    /// </summary>
    /// <param name="tweet">推文实体</param>
    /// <param name="viewerId">查看者用户 ID（未认证时为 <see cref="Guid.Empty"/>）</param>
    /// <returns>true 表示可见</returns>
    public static bool IsVisibleTo(Tweet tweet, Guid viewerId)
    {
        if (tweet.AuthorGuid == viewerId)
            return true;

        if (tweet.Visibility == Visibility.Public)
            return true;

        // Private 仅作者可见；Followers 无关注关系实现，退化为仅作者可见
        return false;
    }
}
