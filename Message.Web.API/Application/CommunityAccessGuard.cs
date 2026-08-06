
namespace Message.Web.API.Application;

/// <summary>
/// 圈子访问守卫。
/// <para>
/// 规则：
/// - 圈子 Feed/详情/成员列表：仅圈子成员可访问；
/// - 圈子帖互动（评论/点赞/收藏/分享/投币/记录查看）：作者本人始终允许，其余必须为圈子成员；
/// - 全局帖（CircleGuid == null）：无圈子限制。
/// </para>
/// </summary>
public static class CommunityAccessGuard
{
    /// <summary>校验调用者是圈子成员，否则抛 UnauthorizedAccessException</summary>
    public static async Task EnsureCircleMemberAsync(
        ICircleRepository circleRepository, Guid circleGuid, Guid userId)
    {
        if (circleGuid == Guid.Empty)
            throw new ArgumentException("圈子ID不能为空", nameof(circleGuid));
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("未认证用户");

        if (!await circleRepository.IsMemberAsync(circleGuid, userId))
            throw new UnauthorizedAccessException("不是圈子成员，无权访问");
    }

    /// <summary>校验调用者可以互动指定帖子（圈子帖需成员身份，作者本人放行）</summary>
    public static async Task EnsureCanInteractWithPostAsync(
        Tweet tweet, Guid userId, ICircleRepository circleRepository)
    {
        if (tweet.CircleGuid is null)
            return;

        // 作者本人始终可互动自己的帖子
        if (tweet.AuthorGuid == userId)
            return;

        if (!await circleRepository.IsMemberAsync(tweet.CircleGuid.Value, userId))
            throw new UnauthorizedAccessException("只有圈子成员可以互动");
    }

    /// <summary>判断帖子对查看者是否可见（圈子帖需成员身份，作者本人放行）</summary>
    public static async Task<bool> IsPostVisibleToAsync(
        Tweet tweet, Guid viewerId, ICircleRepository circleRepository)
    {
        if (tweet.CircleGuid is null)
            return true;
        if (tweet.AuthorGuid == viewerId)
            return true;
        return await circleRepository.IsMemberAsync(tweet.CircleGuid.Value, viewerId);
    }
}
