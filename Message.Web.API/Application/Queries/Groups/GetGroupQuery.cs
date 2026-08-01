namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 获取群组详情查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
public record GetGroupQuery(Guid GroupId) : IRequest<Group?>;

/// <summary>
/// 获取群组详情查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetGroupQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<GetGroupQuery, Group?>
{
    public async Task<Group?> Handler(GetGroupQuery query, CancellationToken cancellationToken)
        => await groupRepository.GetByIdAsync(query.GroupId);
}
