namespace Message.Web.API.Application.Queries.UserInfo;

/// <summary>获取用户资料查询处理程序（不存在时返回默认等级 1/硬币 0，不落库）。</summary>
public class GetMyUserInfoQueryHandler(
    IUserInfoRepository userInfoRepository) : IRequestHandler<GetMyUserInfoQuery, UserInfoDto>
{
    public async Task<UserInfoDto> Handler(GetMyUserInfoQuery query, CancellationToken cancellationToken)
    {
        var userInfo = await userInfoRepository.GetByUserIdAsync(query.UserId);
        if (userInfo is not null)
            return userInfo.ToDto();

        // 未创建过资料：返回默认值（前端可直接展示，首次写操作时再落库）
        return new UserInfoDto
        {
            UserId = query.UserId,
            Level = 1,
            Coins = 0,
            BackgroundCoverUrl = null,
            UpdateTime = DateTimeOffset.UtcNow
        };
    }
}
