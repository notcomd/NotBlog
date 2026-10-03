
namespace Message.Infrastructure.Repository;

/// <summary>群组仓储实现，负责 Groups 与 GroupMembers 的查询与持久化。</summary>
public class GroupRepository(MessageDbContext context) : IGroupRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;
    /// <summary>获取当前数据库上下文。</summary>
    public MessageDbContext Context => context;
    private readonly DbSet<Group> DbSet = context.Groups;

    /// <summary>按群组 ID 获取未解散的群组，不存在时返回 null。</summary>
    public async Task<Group?> GetByIdAsync(Guid groupId)
    {
        return await DbSet
            .FirstOrDefaultAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    /// <summary>按群组 ID 获取未解散的群组（含成员集合）。</summary>
    public async Task<Group?> GetByIdWithMembersAsync(Guid groupId)
    {
        return await DbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    /// <summary>按群主 ID 获取其未解散的群组。</summary>
    public async Task<Group?> GetByOwnerIdAsync(Guid ownerId)
    {
        return await DbSet
            .FirstOrDefaultAsync(g => g.OwnerId == ownerId && !g.IsDismissed);
    }

    /// <summary>按圈子 ID 获取未解散的群组。</summary>
    public async Task<Group?> GetByCircleIdAsync(Guid circleId)
    {
        return await DbSet
            .FirstOrDefaultAsync(g => g.CircleId == circleId && !g.IsDismissed);
    }

    /// <summary>按圈子 ID 获取未解散的群组（含成员集合）。</summary>
    public async Task<Group?> GetByCircleIdWithMembersAsync(Guid circleId)
    {
        return await DbSet
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.CircleId == circleId && !g.IsDismissed);
    }

    /// <summary>获取指定成员已加入且未被封禁的所有未解散群组。</summary>
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

    /// <summary>获取所有公开且未解散的群组，按群名称排序。</summary>
    public async Task<IEnumerable<Group>> GetPublicGroupsAsync()
    {
        return await DbSet
            .Where(g => g.IsPublic && !g.IsDismissed)
            .OrderBy(g => g.GroupName)
            .ToListAsync();
    }

    /// <summary>获取指定成员在群内担任指定角色的所有未解散群组。</summary>
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


    /// <summary>新增群组并返回已跟踪的实体。</summary>
    public async Task<Group> AddAsync(Group group)
    {
        var entry = await DbSet.AddAsync(group);
        return entry.Entity;
    }

    /// <summary>更新群组；仅对未跟踪实体执行更新，避免聚合内新增成员被误标为修改。</summary>
    public Task<Group> UpdateAsync(Group group)
    {
        try
        {
            // 仅对未跟踪实体执行 DbSet.Update；已跟踪实体交由 ChangeTracker 自动检测修改。
            // 修复（2026-08-15）：DbSet.Update 会递归遍历对象图，把聚合内「新增」子实体
            // （如 AddMember 新建的 GroupMember）从 Added 强制改为 Modified，SaveChanges 时
            // 对不存在的行生成 UPDATE → 影响 0 行 → DbUpdateConcurrencyException。
            if (Context.Entry(group).State == EntityState.Detached)
                DbSet.Update(group);
            return Task.FromResult(group);
        }
        catch (Exception exception)
        {
            return Task.FromException<Group>(exception);
        }
    }

    /// <summary>解散（软删除）指定群组。</summary>
    public async Task DeleteAsync(Guid groupId)
    {
        var group = await GetByIdAsync(groupId);
        if (group is not null)
        {
            group.Dismiss();
            DbSet.Update(group);
        }
    }

    /// <summary>判断指定未解散群组是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid groupId)
    {
        return await DbSet.AnyAsync(g => g.GroupId == groupId && !g.IsDismissed);
    }

    /// <summary>判断指定用户是否为该群组未封禁的成员。</summary>
    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && !gm.IsBanned);
    }

    /// <summary>判断指定用户是否为该未解散群组的群主。</summary>
    public async Task<bool> IsOwnerAsync(Guid groupId, Guid userId)
    {
        return await DbSet
            .AnyAsync(g => g.GroupId == groupId && g.OwnerId == userId && !g.IsDismissed);
    }

    /// <summary>判断指定用户是否为该群组的管理员或群主。</summary>
    public async Task<bool> IsAdminAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .AnyAsync(gm => gm.GroupId == groupId &&
                            gm.UserId == userId &&
                            (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.Owner));
    }

    /// <summary>统计群组内未封禁的成员数量。</summary>
    public async Task<int> GetMemberCountAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .CountAsync(gm => gm.GroupId == groupId && !gm.IsBanned);
    }

    /// <summary>统计指定群主拥有的未解散群组数量。</summary>
    public async Task<int> GetGroupCountByOwnerAsync(Guid ownerId)
    {
        return await DbSet
            .CountAsync(g => g.OwnerId == ownerId && !g.IsDismissed);
    }

    /// <summary>统计指定成员已加入且未被封禁的未解散群组数量。</summary>
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

    /// <summary>将群组所有权转移给新的群主。</summary>
    public async Task TransferOwnershipAsync(Guid groupId, Guid newOwnerId)
    {
        var group = await GetByIdWithMembersAsync(groupId);
        if (group is not null)
        {
            group.TransferOwnership(newOwnerId);
            DbSet.Update(group);
        }
    }

    /// <summary>获取指定用户在群组内的成员记录，不存在时返回 null。</summary>
    public async Task<GroupMember?> GetMemberAsync(Guid groupId, Guid userId)
    {
        return await Context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
    }

    /// <summary>获取群组内全部未封禁成员，按加入时间排序。</summary>
    public async Task<IEnumerable<GroupMember>> GetMembersAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.GroupId == groupId && !gm.IsBanned)
            .OrderBy(gm => gm.JoinTime)
            .ToListAsync();
    }

    /// <summary>获取群组内的管理员与群主成员。</summary>
    public async Task<IEnumerable<GroupMember>> GetAdminsAsync(Guid groupId)
    {
        return await Context.GroupMembers
            .Where(gm => gm.GroupId == groupId &&
                         (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.Owner))
            .ToListAsync();
    }

    /// <summary>获取指定用户有权发送消息（未被封禁且成员状态允许）的所有未解散群组。</summary>
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