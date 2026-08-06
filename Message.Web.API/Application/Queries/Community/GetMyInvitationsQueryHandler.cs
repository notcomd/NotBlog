namespace Message.Web.API.Application.Queries.Community;

/// <summary>我收到的直邀查询处理程序。</summary>
public class GetMyInvitationsQueryHandler(
    ICircleInvitationRepository invitationRepository) : IRequestHandler<GetMyInvitationsQuery, PagedResult<CircleInvitation>>
{
    public async Task<PagedResult<CircleInvitation>> Handler(GetMyInvitationsQuery query, CancellationToken cancellationToken)
    {
        var items = await invitationRepository.GetByInviteeAsync(query.UserId, true, query.Page, query.PageSize);
        var total = await invitationRepository.GetCountByInviteeAsync(query.UserId, true);

        return new PagedResult<CircleInvitation>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
