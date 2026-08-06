namespace Message.Web.API.Application.Queries.Community;

/// <summary>圈子详情查询处理程序。</summary>
public class GetCircleQueryHandler(
    ICircleRepository circleRepository) : IRequestHandler<GetCircleQuery, CircleDetailResult>
{
    public async Task<CircleDetailResult> Handler(GetCircleQuery query, CancellationToken cancellationToken)
    {
        var circle = await circleRepository.GetByIdAsync(query.CircleGuid);
        if (circle is null)
            return new CircleDetailResult(null, false, null);

        var member = await circleRepository.GetMemberAsync(query.CircleGuid, query.CurrentUserId);
        var isMember = member is not null && member.Status == CircleMemberStatus.Active;
        return new CircleDetailResult(circle, isMember, member?.Role);
    }
}
