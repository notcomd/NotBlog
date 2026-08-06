namespace Message.Web.API.Application.Queries.Groups;
/// <summary>
/// 获取群组成员列表查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class GetGroupMembersQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<GetGroupMembersQuery, IEnumerable<GroupMember>>
{
    public async Task<IEnumerable<GroupMember>> Handler(GetGroupMembersQuery query, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdWithMembersAsync(query.GroupId);
        return group?.Members ?? Enumerable.Empty<GroupMember>();
    }
}
