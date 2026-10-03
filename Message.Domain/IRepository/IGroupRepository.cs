
namespace Message.Domain.IRepository;

/// <summary>
/// 群组仓储接口（Group 聚合根）。
/// </summary>
public interface IGroupRepository : IRepository<Group, IUnitOfWork>
{
    /// <summary>按群组 ID 查询（不含成员集合，不存在返回 null）</summary>
    Task<Group?> GetByIdAsync(Guid groupId);
    /// <summary>
    ///   加载聚合根及其成员集合（用于需要修改成员的场景）
    /// </summary>
    Task<Group?> GetByIdWithMembersAsync(Guid groupId);
    /// <summary>按群主 ID 查询其拥有的群组（不存在返回 null）</summary>
    Task<Group?> GetByOwnerIdAsync(Guid ownerId);
    /// <summary>按所属社区查询群组（社区群组反查；普通群聊无此关联，返回 null）</summary>
    Task<Group?> GetByCircleIdAsync(Guid circleId);
    /// <summary>按所属社区加载群组及其成员集合（用于随社区成员同步增减）</summary>
    Task<Group?> GetByCircleIdWithMembersAsync(Guid circleId);
    /// <summary>查询用户加入的群组</summary>
    Task<IEnumerable<Group>> GetByMemberIdAsync(Guid memberId);
    /// <summary>查询公开群组列表</summary>
    Task<IEnumerable<Group>> GetPublicGroupsAsync();
    /// <summary>按成员身份角色查询用户所在群组</summary>
    Task<IEnumerable<Group>> GetByMemberIdAndRoleAsync(Guid memberId, GroupMemberRole role);
    /// <summary>按关键词分页搜索群组</summary>
    Task<IEnumerable<Group>> SearchAsync(string searchTerm, int page, int pageSize);
    /// <summary>搜索群组总数（F-06 分页 TotalCount）</summary>
    Task<int> SearchCountAsync(string searchTerm);
    /// <summary>新增群组</summary>
    Task<Group> AddAsync(Group group);
    /// <summary>更新群组</summary>
    Task<Group> UpdateAsync(Group group);
    /// <summary>删除群组</summary>
    Task DeleteAsync(Guid groupId);
    /// <summary>判断群组是否存在</summary>
    Task<bool> ExistsAsync(Guid groupId);
    /// <summary>判断用户是否为群成员</summary>
    Task<bool> IsMemberAsync(Guid groupId, Guid userId);
    /// <summary>判断用户是否为群主</summary>
    Task<bool> IsOwnerAsync(Guid groupId, Guid userId);
    /// <summary>判断用户是否为群管理员</summary>
    Task<bool> IsAdminAsync(Guid groupId, Guid userId);
    /// <summary>获取群成员数量</summary>
    Task<int> GetMemberCountAsync(Guid groupId);
    /// <summary>获取用户拥有的群组数量</summary>
    Task<int> GetGroupCountByOwnerAsync(Guid ownerId);
    /// <summary>获取用户加入的群组数量</summary>
    Task<int> GetGroupCountByMemberAsync(Guid memberId);
    /// <summary>查询成员（读操作，非聚合根遍历）</summary>
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);
    /// <summary>查询成员列表（读操作）</summary>
    Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId);
    /// <summary>查询管理员列表（读操作）</summary>
    Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId);
    /// <summary>转移群主所有权</summary>
    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId);
    /// <summary>查询用户可发送消息的群组列表（成员身份且未被禁言等）</summary>
    Task<IEnumerable<Group>> GetGroupsWhereUserCanSendMessageAsync(Guid userId);
}