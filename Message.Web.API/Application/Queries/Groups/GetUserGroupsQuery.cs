namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 获取用户加入的群组列表查询。
/// </summary>
/// <param name="UserId">用户 ID</param>
public record GetUserGroupsQuery(Guid UserId) : IRequest<IEnumerable<Group>>;

/// <summary>
/// 获取用户加入的群组列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetUserGroupsQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<GetUserGroupsQuery, IEnumerable<Group>>
{
    public async Task<IEnumerable<Group>> Handler(GetUserGroupsQuery query, CancellationToken cancellationToken)
        => await groupRepository.GetByMemberIdAsync(query.UserId);
}
