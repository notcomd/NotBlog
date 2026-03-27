using Message.Domain.Entities.Group;
using Message.Domain.Enums;

namespace Message.Domain.IProvider;

public interface IGroupProvider
{
    Task<Group> CreateGroupAsync(Guid ownerId, string groupName, int maxMembers = 500, bool isPublic = false);

    Task<Group?> GetGroupAsync(Guid groupId);

    Task<Group?> GetGroupByOwnerAsync(Guid ownerId);

    Task<IEnumerable<Group>> GetUserGroupsAsync(Guid memberId);

    Task<IEnumerable<Group>> GetPublicGroupsAsync();

    Task<IEnumerable<Group>> GetUserGroupsByRoleAsync(Guid memberId, GroupMemberRole role);

    Task<IEnumerable<Group>> SearchGroupsAsync(string searchTerm, int page = 1, int pageSize = 20);

    Task<int> GetGroupCountByOwnerAsync(Guid ownerId);

    Task<int> GetGroupCountByMemberAsync(Guid memberId);

    Task<int> GetMemberCountAsync(Guid groupId);

    Task<bool> GroupExistsAsync(Guid groupId);

    Task<bool> IsMemberAsync(Guid groupId, Guid userId);

    Task<bool> IsOwnerAsync(Guid groupId, Guid userId);

    Task<bool> IsAdminAsync(Guid groupId, Guid userId);

    Task<bool> CanSendMessageAsync(Guid groupId, Guid userId);

    Task<bool> HasPermissionAsync(Guid groupId, Guid userId, GroupPermission permission);

    Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId);

    Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId);

    Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId);

    Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role = GroupMemberRole.Member);

    Task RemoveMemberAsync(Guid groupId, Guid userId);

    Task PromoteToAdminAsync(Guid groupId, Guid userId);

    Task DemoteToMemberAsync(Guid groupId, Guid userId);

    Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId);

    Task MuteMemberAsync(Guid groupId, Guid userId, TimeSpan duration);

    Task UnmuteMemberAsync(Guid groupId, Guid userId);

    Task BanMemberAsync(Guid groupId, Guid userId);

    Task UnbanMemberAsync(Guid groupId, Guid userId);

    Task UpdateGroupInfoAsync(Guid groupId, string groupName, string? description);

    Task UpdateGroupAvatarAsync(Guid groupId, Uri avatarUri);

    Task UpdateGroupPermissionsAsync(Guid groupId, bool allowMemberInvite, bool allowMemberEditInfo);

    Task DismissGroupAsync(Guid groupId);
}