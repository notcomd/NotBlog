namespace Message.Web.API.Application.Queries.Community;

/// <summary>关注列表查询处理程序。</summary>
public class GetFollowingQueryHandler(
    IUserFollowRepository followRepository) : IRequestHandler<GetFollowingQuery, PagedResult<UserFollow>>
{
    public async Task<PagedResult<UserFollow>> Handler(GetFollowingQuery query, CancellationToken cancellationToken)
    {
        var items = await followRepository.GetFollowingAsync(query.UserGuid, query.Page, query.PageSize);
        var total = await followRepository.GetFollowingCountAsync(query.UserGuid);

        return new PagedResult<UserFollow>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
