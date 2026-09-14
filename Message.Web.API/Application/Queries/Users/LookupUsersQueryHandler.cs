namespace Message.Web.API.Application.Queries.Users;
using UserInfoEntity = Message.Domain.Entities.User.UserInfo;

/// <summary>
/// 查找用户查询处理程序：含 @ 按邮箱精确取单条，否则按昵称精确匹配（重名取前 Limit 条）。
/// <para>返回 <see cref="UserBriefDto"/> 仅含 UserGuid/UserName/Avatar，
/// 不回传邮箱、等级、角色等字段，避免经由好友查找接口泄露个人资料。</para>
/// </summary>
public class LookupUsersQueryHandler(IUserInfoRepository userInfoRepository)
    : IRequestHandler<LookupUsersQuery, IEnumerable<UserBriefDto>>
{
    /// <summary>邮箱判定字符：含 @ 即按邮箱精确匹配（添加好友的主要输入路径）</summary>
    private const char EmailIndicator = '@';

    public async Task<IEnumerable<UserBriefDto>> Handler(LookupUsersQuery query, CancellationToken cancellationToken)
    {
        var keyword = query.Keyword.Trim();

        if (keyword.Contains(EmailIndicator))
        {
            var byEmail = await userInfoRepository.GetByEmailAsync(keyword);
            return byEmail is null ? [] : [ToBrief(byEmail)];
        }

        var byNickName = await userInfoRepository.GetByNickNameAsync(keyword, query.Limit);
        return byNickName.Select(ToBrief);
    }

    private static UserBriefDto ToBrief(UserInfoEntity user) => new()
    {
        UserGuid = user.UserId,
        UserName = user.NickName ?? string.Empty,
        Avatar = user.AvatarUrl?.ToString()
    };
}
