using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.SeedWork;

namespace Message.Domain.IRepository;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid groupId);
    Task<Group?> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Group>> GetByMemberIdAsync(Guid memberId);
    Task<IEnumerable<Group>> GetPublicGroupsAsync();
    Task<IEnumerable<Group>> GetByMemberIdAndRoleAsync(Guid memberId, GroupMemberRole role);
    Task<IEnumerable<Group>> SearchAsync(string searchTerm, int page, int pageSize);
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
    Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role = GroupMemberRole.Member);
    Task RemoveMemberAsync(Guid groupId, Guid userId);
    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);
    Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId);
    Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId);
    Task PromoteToAdminAsync(Guid groupId, Guid userId);
    Task DemoteToMemberAsync(Guid groupId, Guid userId);
    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId);
    Task MuteMemberAsync(Guid groupId, Guid userId, TimeSpan duration);
    Task UnmuteMemberAsync(Guid groupId, Guid userId);
    Task BanMemberAsync(Guid groupId, Guid userId);
    Task UnbanMemberAsync(Guid groupId, Guid userId);
    Task<IEnumerable<Group>> GetGroupsWhereUserCanSendMessageAsync(Guid userId);
}