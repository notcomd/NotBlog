using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.SeedWork;

namespace Message.Infrastructure.Provider;

public class GroupProvider(
    IGroupRepository groupRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IGroupProvider
{
    public async Task<Group> CreateGroupAsync(Guid ownerId, string groupName, int maxMembers = 500,
        bool isPublic = false)
    {
        var owner = await userRepository.GetByIdAsync(ownerId);
        if (owner == null)
            throw new KeyNotFoundException("群主不存在");

        var group = new Group(ownerId, groupName, maxMembers, isPublic);
        await groupRepository.AddAsync(group);
        await unitOfWork.SavaEntitiesAsync();

        return group;
    }

    public async Task<Group?> GetGroupAsync(Guid groupId)
    {
        return await groupRepository.GetByIdAsync(groupId);
    }

    public async Task<Group?> GetGroupByOwnerAsync(Guid ownerId)
    {
        return await groupRepository.GetByOwnerIdAsync(ownerId);
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(Guid memberId)
    {
        return await groupRepository.GetByMemberIdAsync(memberId);
    }

    public async Task<IEnumerable<Group>> GetPublicGroupsAsync()
    {
        return await groupRepository.GetPublicGroupsAsync();
    }

    public async Task<IEnumerable<Group>> GetUserGroupsByRoleAsync(Guid memberId, GroupMemberRole role)
    {
        return await groupRepository.GetByMemberIdAndRoleAsync(memberId, role);
    }

    public async Task<IEnumerable<Group>> SearchGroupsAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        return await groupRepository.SearchAsync(searchTerm, page, pageSize);
    }

    public async Task<int> GetGroupCountByOwnerAsync(Guid ownerId)
    {
        return await groupRepository.GetGroupCountByOwnerAsync(ownerId);
    }

    public async Task<int> GetGroupCountByMemberAsync(Guid memberId)
    {
        return await groupRepository.GetGroupCountByMemberAsync(memberId);
    }

    public async Task<int> GetMemberCountAsync(Guid groupId)
    {
        return await groupRepository.GetMemberCountAsync(groupId);
    }

    public async Task<bool> GroupExistsAsync(Guid groupId)
    {
        return await groupRepository.ExistsAsync(groupId);
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId)
    {
        return await groupRepository.IsMemberAsync(groupId, userId);
    }

    public async Task<bool> IsOwnerAsync(Guid groupId, Guid userId)
    {
        return await groupRepository.IsOwnerAsync(groupId, userId);
    }

    public async Task<bool> IsAdminAsync(Guid groupId, Guid userId)
    {
        return await groupRepository.IsAdminAsync(groupId, userId);
    }

    public async Task<bool> CanSendMessageAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        return member?.CanSendMessage() ?? false;
    }

    public async Task<bool> HasPermissionAsync(Guid groupId, Guid userId, GroupPermission permission)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            return false;

        return group.HasPermission(userId, permission);
    }

    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId)
    {
        return await groupRepository.GetMemberAsync(groupId, userId);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId)
    {
        return await groupRepository.GetMembersAsync(groupId);
    }

    public async Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId)
    {
        return await groupRepository.GetAdminsAsync(groupId);
    }

    public async Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role = GroupMemberRole.Member)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var user = await userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        group.AddMember(userId, role);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.RemoveMember(userId);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task PromoteToAdminAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.PromoteToAdmin();
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task DemoteToMemberAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.DemoteToMember();
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.TransferOwnership(newOwnerId);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task MuteMemberAsync(Guid groupId, Guid userId, TimeSpan duration)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Mute(duration);
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnmuteMemberAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Unmute();
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task BanMemberAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Ban();
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnbanMemberAsync(Guid groupId, Guid userId)
    {
        var member = await groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Unban();
        await groupRepository.UpdateAsync(await groupRepository.GetByIdAsync(groupId) ??
                                          throw new KeyNotFoundException("群组不存在"));
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupInfoAsync(Guid groupId, string groupName, string? description)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdateGroupInfo(groupName, description, null);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupAvatarAsync(Guid groupId, Uri avatarUri)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdateGroupInfo(group.GroupName, null, avatarUri);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupPermissionsAsync(Guid groupId, bool allowMemberInvite, bool allowMemberEditInfo)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdatePermissions(allowMemberInvite, allowMemberEditInfo);
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }

    public async Task DismissGroupAsync(Guid groupId)
    {
        var group = await groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.Dismiss();
        await groupRepository.UpdateAsync(group);
        await unitOfWork.SavaEntitiesAsync();
    }
}