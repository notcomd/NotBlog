namespace Message.Web.API.Application.Queries.Groups;

/// <summary>
/// 检查用户是否为群组成员查询。
/// </summary>
/// <param name="GroupId">群组 ID</param>
/// <param name="UserId">用户 ID</param>
public record IsGroupMemberQuery(Guid GroupId, Guid UserId) : IRequest<bool>;

/// <summary>
/// 检查用户是否为群组成员查询处理程序。
/// <para>纯查询：不修改任何数据状态。</para>
/// </summary>
public class IsGroupMemberQueryHandler(
    IGroupRepository groupRepository) : IRequestHandler<IsGroupMemberQuery, bool>
{
    public async Task<bool> Handler(IsGroupMemberQuery query, CancellationToken cancellationToken)
        => await groupRepository.IsMemberAsync(query.GroupId, query.UserId);
}
