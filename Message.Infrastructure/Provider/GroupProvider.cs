using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.SeedWork;

namespace Message.Infrastructure.Provider;

public class GroupProvider : IGroupProvider
{
    private readonly IGroupRepository _groupRepository;

    private readonly IUnitOfWork _unitOfWork;

    public GroupProvider(
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<Group> CreateGroupAsync(Guid ownerId, string groupName, int maxMembers = 500,
        bool isPublic = false)
    {
        var owner = await _userRepository.GetByIdAsync(ownerId);
        if (owner == null)
            throw new KeyNotFoundException("群主不存在");

        var group = new Group(ownerId, groupName, maxMembers, isPublic);
        await _groupRepository.AddAsync(group);
        await _unitOfWork.SavaEntitiesAsync();

        return group;
    }

    public async Task<Group?> GetGroupAsync(Guid groupId)
    {
        return await _groupRepository.GetByIdAsync(groupId);
    }

    public async Task<Group?> GetGroupByOwnerAsync(Guid ownerId)
    {
        return await _groupRepository.GetByOwnerIdAsync(ownerId);
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(Guid memberId)
    {
        return await _groupRepository.GetByMemberIdAsync(memberId);
    }

    public async Task<IEnumerable<Group>> GetPublicGroupsAsync()
    {
        return await _groupRepository.GetPublicGroupsAsync();
    }

    public async Task<IEnumerable<Group>> GetUserGroupsByRoleAsync(Guid memberId, GroupMemberRole role)
    {
        return await _groupRepository.GetByMemberIdAndRoleAsync(memberId, role);
    }

    public async Task<IEnumerable<Group>> SearchGroupsAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        return await _groupRepository.SearchAsync(searchTerm, page, pageSize);
    }

    public async Task<int> GetGroupCountByOwnerAsync(Guid ownerId)
    {
        return await _groupRepository.GetGroupCountByOwnerAsync(ownerId);
    }

    public async Task<int> GetGroupCountByMemberAsync(Guid memberId)
    {
        return await _groupRepository.GetGroupCountByMemberAsync(memberId);
    }

    public async Task<int> GetMemberCountAsync(Guid groupId)
    {
        return await _groupRepository.GetMemberCountAsync(groupId);
    }

    public async Task<bool> GroupExistsAsync(Guid groupId)
    {
        return await _groupRepository.ExistsAsync(groupId);
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository.IsMemberAsync(groupId, userId);
    }

    public async Task<bool> IsOwnerAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository.IsOwnerAsync(groupId, userId);
    }

    public async Task<bool> IsAdminAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository.IsAdminAsync(groupId, userId);
    }

    public async Task<bool> CanSendMessageAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        return member?.CanSendMessage() ?? false;
    }

    public async Task<bool> HasPermissionAsync(Guid groupId, Guid userId, GroupPermission permission)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            return false;

        return group.HasPermission(userId, permission);
    }

    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId)
    {
        return await _groupRepository.GetMemberAsync(groupId, userId);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId)
    {
        return await _groupRepository.GetMembersAsync(groupId);
    }

    public async Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId)
    {
        return await _groupRepository.GetAdminsAsync(groupId);
    }

    public async Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role = GroupMemberRole.Member)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("用户不存在");

        group.AddMember(userId, role);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.RemoveMember(userId);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task PromoteToAdminAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.PromoteToAdmin();
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task DemoteToMemberAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.DemoteToMember();
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.TransferOwnership(newOwnerId);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task MuteMemberAsync(Guid groupId, Guid userId, TimeSpan duration)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Mute(duration);
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnmuteMemberAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Unmute();
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task BanMemberAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Ban();
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UnbanMemberAsync(Guid groupId, Guid userId)
    {
        var member = await _groupRepository.GetMemberAsync(groupId, userId);
        if (member == null)
            throw new KeyNotFoundException("成员不存在");

        member.Unban();
        await _groupRepository.UpdateAsync(await _groupRepository.GetByIdAsync(groupId) ??
                                           throw new KeyNotFoundException("群组不存在"));
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupInfoAsync(Guid groupId, string groupName, string? description)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdateGroupInfo(groupName, description, null);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupAvatarAsync(Guid groupId, Uri avatarUri)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdateGroupInfo(group.GroupName, null, avatarUri);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task UpdateGroupPermissionsAsync(Guid groupId, bool allowMemberInvite, bool allowMemberEditInfo)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.UpdatePermissions(allowMemberInvite, allowMemberEditInfo);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }

    public async Task DismissGroupAsync(Guid groupId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new KeyNotFoundException("群组不存在");

        group.Dismiss();
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SavaEntitiesAsync();
    }
}