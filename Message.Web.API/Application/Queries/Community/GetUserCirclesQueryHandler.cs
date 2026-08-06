namespace Message.Web.API.Application.Queries.Community;

/// <summary>我的圈子查询处理程序。</summary>
public class GetUserCirclesQueryHandler(
    ICircleRepository circleRepository) : IRequestHandler<GetUserCirclesQuery, IEnumerable<Circle>>
{
    public async Task<IEnumerable<Circle>> Handler(GetUserCirclesQuery query, CancellationToken cancellationToken)
    {
        return await circleRepository.GetByMemberAsync(query.UserId);
    }
}
