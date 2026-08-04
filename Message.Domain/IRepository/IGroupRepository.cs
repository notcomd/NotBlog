using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Commons.SeedWork;

namespace Message.Domain.IRepository;

public interface IGroupRepository : IRepository<Group, IUnitOfWork>
{
    Task<Group?> GetByIdAsync(Guid groupId);
    /// <summary>
    ///   加载聚合根及其成员集合（用于需要修改成员的场景）
    /// </summary>
    Task<Group?> GetByIdWithMembersAsync(Guid groupId);
    Task<Group?> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Group>> GetByMemberIdAsync(Guid memberId);
    Task<IEnumerable<Group>> GetPublicGroupsAsync();
    Task<IEnumerable<Group>> GetByMemberIdAndRoleAsync(Guid memberId, GroupMemberRole role);
    Task<IEnumerable<Group>> SearchAsync(string searchTerm, int page, int pageSize);
    /// <summary>搜索群组总数（F-06 分页 TotalCount）</summary>
    Task<int> SearchCountAsync(string searchTerm);
    Task<Group> AddAsync(Group group);
    Task<Group> UpdateAsync(Group group);
    Task DeleteAsync(Guid groupId);
    Task<bool> ExistsAsync(Guid groupId);
    Task<bool> IsMemberAsync(Guid groupId, Guid userId);
    Task<bool> IsOwnerAsync(Guid groupId, Guid userId);
    Task<bool> IsAdminAsync(Guid groupId, Guid userId);
    Task<int> GetMemberCountAsync(Guid groupId);
    Task<int> GetGroupCountByOwnerAsync(Guid ownerId);
    Task<int> GetGroupCountByMemberAsync(Guid memberId);
    /// <summary>查询成员（读操作，非聚合根遍历）</summary>
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);
    /// <summary>查询成员列表（读操作）</summary>
    Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId);
    /// <summary>查询管理员列表（读操作）</summary>
    Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId);
    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId);
    Task<IEnumerable<Group>> GetGroupsWhereUserCanSendMessageAsync(Guid userId);
}