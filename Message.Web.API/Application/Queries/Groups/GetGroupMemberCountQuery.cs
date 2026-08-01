namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 获取群组成员数量查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record GetGroupMemberCountQuery(Guid GroupId) : IRequest<int>;

/// <summary>
/// 获取群组成员数量查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetGroupMemberCountQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<GetGroupMemberCountQuery, int>
{
    public async Task<int> Handler(GetGroupMemberCountQuery query, CancellationToken cancellationToken)
        => await groupRepository.GetMemberCountAsync(query.GroupId);
}
