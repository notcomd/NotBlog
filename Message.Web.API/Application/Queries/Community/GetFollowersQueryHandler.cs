namespace Message.Web.API.Application.Queries.Community;

/// <summary>粉丝列表查询处理程序。</summary>
public class GetFollowersQueryHandler(
    IUserFollowRepository followRepository) : IRequestHandler<GetFollowersQuery, PagedResult<UserFollow>>
{
    public async Task<PagedResult<UserFollow>> Handler(GetFollowersQuery query, CancellationToken cancellationToken)
    {
        var items = await followRepository.GetFollowersAsync(query.UserGuid, query.Page, query.PageSize);
        var total = await followRepository.GetFollowerCountAsync(query.UserGuid);

        return new PagedResult<UserFollow>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
