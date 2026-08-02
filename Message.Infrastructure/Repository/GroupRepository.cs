using Message.Infrastructure.EntityFramework;

namespace Message.Infrastructure.Repository;

public class GroupRepository(MessageDbContext context) : IGroupRepository
{
    public IUnitOfWork UnitOfWork => context;
    public MessageDbContext Context => context;
    private readonly DbSet<Group> DbSet = context.Groups;

    public async Task<Group?> GetByIdAsync(Guid groupId)
    {
        return await DbSet
            .FirstOrDefaultAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    public async Task<Group?> GetByIdWithMembersAsync(Guid groupId)
    {
        return await DbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    public async Task<Group?> GetByOwnerIdAsync(Guid ownerId)
    {
        return await DbSet
            .FirstOrDefaultAsync(g => g.OwnerId == ownerId && !g.IsDismissed);
    }

    public async Task<IEnumerable<Group>> GetByMemberIdAsync(Guid memberId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.UserId == memberId && !gm.IsBanned)
            .Join(DbSet,
                gm => gm.GroupId,
                g => g.GroupId,
                (gm, g) => g)
            .Where(g => !g.IsDismissed)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetPublicGroupsAsync()
    {
        return await DbSet
            .Where(g => g.IsPublic && !g.IsDismissed)
            .OrderBy(g => g.GroupName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetByMemberIdAndRoleAsync(Guid memberId, GroupMemberRole role)
    {
        return await Context.GroupMembers
            .Where(gm => gm.UserId == memberId && gm.Role == role)
            .Join(DbSet,
                gm => gm.GroupId,
                g => g.GroupId,
                (gm, g) => g)
            .Where(g => !g.IsDismissed)
            .ToListAsync();
    }

    /// <summary>
    ///   搜索群组
    /// </summary>
    /// <param name="searchTerm"></param>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Group>> SearchAsync(string searchTerm, int page, int pageSize)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet
            .Where(g => g.IsPublic && !g.IsDismissed &&
                        (g.GroupName.Contains(searchTerm) ||
                         (g.Description != null && g.Description.Contains(searchTerm))));

        // F-06：真正分页（此前未应用 Skip/Take，返回全量数据）
        return await query.OrderBy(g => g.GroupName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// 搜索群组总数（F-06 分页 TotalCount）
    /// </summary>
    public async Task<int> SearchCountAsync(string searchTerm)
    {
        return await DbSet.CountAsync(g =>
            g.IsPublic && !g.IsDismissed &&
            (g.GroupName.Contains(searchTerm) ||
             (g.Description != null && g.Description.Contains(searchTerm))));
    }


    public async Task<Group> AddAsync(Group group)
    {
        var entry = await DbSet.AddAsync(group);
        return entry.Entity;
    }

    public Task<Group> UpdateAsync(Group group)
    {
        try
        {
            var entry = DbSet.Update(group);
            return Task.FromResult(entry.Entity);
        }
        catch (Exception exception)
        {
            return Task.FromException<Group>(exception);
        }
    }

    public async Task DeleteAsync(Guid groupId)
    {
        var group = await GetByIdAsync(groupId);
        if (group is not null)
        {
            group.Dismiss();
            DbSet.Update(group);
        }
    }

    public async Task<bool> ExistsAsync(Guid groupId)
    {
        return await DbSet.AnyAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && !gm.IsBanned);
    }

    public async Task<bool> IsOwnerAsync(Guid groupId, Guid userId)
    {
        return await DbSet
            .AnyAsync(g => g.GroupId == groupId && g.OwnerId == userId && !g.IsDismissed);
    }

    public async Task<bool> IsAdminAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId &&
                            gm.UserId == userId &&
                            (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.Owner));
    }

    public async Task<int> GetMemberCountAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .CountAsync(gm => gm.GroupId == groupId && !gm.IsBanned);
    }

    public async Task<int> GetGroupCountByOwnerAsync(Guid ownerId)
    {
        return await DbSet
            .CountAsync(g => g.OwnerId == ownerId && !g.IsDismissed);
    }

    public async Task<int> GetGroupCountByMemberAsync(Guid memberId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.UserId == memberId && !gm.IsBanned)
            .Join(DbSet,
                gm => gm.GroupId,
                g => g.GroupId,
                (gm, g) => g)
            .CountAsync(g => !g.IsDismissed);
    }

    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId)
    {
        var group = await GetByIdWithMembersAsync(groupId);
        if (group is not null)
        {
            group.TransferOwnership(newOwnerId);
            DbSet.Update(group);
        }
    }

    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    public async Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.GroupId == groupId && !gm.IsBanned)
            .OrderBy(gm => gm.JoinTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.GroupId == groupId &&
                         (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.Owner))
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetGroupsWhereUserCanSendMessageAsync(Guid userId)
    {
        var memberGroupIds = await Context.GroupMembers
            .Where(gm => gm.UserId == userId && !gm.IsBanned && gm.CanSendMessage())
            .Select(gm => gm.GroupId)
            .ToListAsync();

        return await DbSet
            .Where(g => memberGroupIds.Contains(g.GroupId) && !g.IsDismissed)
            .ToListAsync();
    }
}