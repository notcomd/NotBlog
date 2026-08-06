namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子成员列表查询处理程序。</summary>
public class GetCircleMembersQueryHandler(
    ICircleRepository circleRepository) : IRequestHandler<GetCircleMembersQuery, PagedResult<CircleMember>>
{
    public async Task<PagedResult<CircleMember>> Handler(GetCircleMembersQuery query, CancellationToken cancellationToken)
    {
        await CommunityAccessGuard.EnsureCircleMemberAsync(circleRepository, query.CircleGuid, query.CurrentUserId);

        var items = await circleRepository.GetMembersAsync(query.CircleGuid, query.Page, query.PageSize);
        var total = await circleRepository.GetMemberCountAsync(query.CircleGuid);

        return new PagedResult<CircleMember>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
