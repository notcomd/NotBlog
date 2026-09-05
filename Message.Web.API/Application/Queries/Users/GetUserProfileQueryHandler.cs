namespace Message.Web.API.Application.Queries.Users;

/// <summary>
/// 用户公开信息查询处理程序：
/// 资料（昵称/头像/bio，UserInfo 未创建时返回空） + 关注/粉丝计数 + 已发布作品数（与列表同可见性口径）
/// + 作品获赞总数 + 当前查看者是否已关注对方。
/// </summary>
public class GetUserProfileQueryHandler(
    IUserInfoRepository userInfoRepository,
    IUserFollowRepository userFollowRepository,
    ITweetRepository tweetRepository) : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handler(GetUserProfileQuery query, CancellationToken cancellationToken)
    {
        var targetGuid = query.UserGuid;
        var viewerId = query.ViewerId;
        var viewerFollowingIds = viewerId == Guid.Empty
            ? []
            : (await userFollowRepository.GetFollowingIdsAsync(viewerId)).ToList();

        // 资料（不存在则返回空字段，前端降级展示；不落库）
        var userInfo = await userInfoRepository.GetByUserIdAsync(targetGuid);

        // 关注/粉丝统计
        var followingCount = await userFollowRepository.GetFollowingCountAsync(targetGuid);
        var followerCount = await userFollowRepository.GetFollowerCountAsync(targetGuid);

        // 作品口径：与「TA 的作品」列表一致（可见性过滤）
        var postCount = await tweetRepository.GetCountByAuthorAsync(targetGuid, viewerId, viewerFollowingIds);

        // 获赞总数：已发布作品 LikeCount 之和（公开口径）
        var likeTotal = await tweetRepository.GetLikeTotalByAuthorAsync(targetGuid);

        var isFollowing = viewerId != Guid.Empty
                          && await userFollowRepository.ExistsAsync(viewerId, targetGuid);

        return new UserProfileDto
        {
            UserGuid = targetGuid,
            NickName = userInfo?.NickName,
            Bio = userInfo?.Bio,
            AvatarUrl = userInfo?.AvatarUrl,
            FollowingCount = followingCount,
            FollowerCount = followerCount,
            PostCount = postCount,
            LikeTotal = likeTotal,
            IsFollowing = isFollowing
        };
    }
}