namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子邀请列表查询处理程序。</summary>
public class GetCircleInvitationsQueryHandler(
    ICircleRepository circleRepository,
    ICircleInvitationRepository invitationRepository) : IRequestHandler<GetCircleInvitationsQuery, PagedResult<CircleInvitation>>
{
    public async Task<PagedResult<CircleInvitation>> Handler(GetCircleInvitationsQuery query, CancellationToken cancellationToken)
    {
        var member = await circleRepository.GetMemberAsync(query.CircleGuid, query.OperatorGuid);
        if (member is null || member.Role == CircleMemberRole.Member)
            throw new UnauthorizedAccessException("只有圈主或管理员可以查看邀请列表");

        var items = await invitationRepository.GetByCircleAsync(query.CircleGuid, query.Page, query.PageSize);
        var total = await invitationRepository.GetCountByCircleAsync(query.CircleGuid);

        return new PagedResult<CircleInvitation>
        {
            Items = items.ToList(),
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
