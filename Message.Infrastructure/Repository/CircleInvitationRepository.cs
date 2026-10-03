
namespace Message.Infrastructure.Repository;

/// <summary>圈子邀请仓储实现，负责 CircleInvitations 表的查询与持久化。</summary>
public class CircleInvitationRepository(MessageDbContext context) : ICircleInvitationRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<CircleInvitation> DbSet = context.CircleInvitations;

    /// <summary>按邀请标识获取邀请记录，不存在时返回 null。</summary>
    public async Task<CircleInvitation?> GetByIdAsync(Guid inviteGuid)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.InviteGuid == inviteGuid);
    }

    /// <summary>按邀请码获取邀请记录，不存在时返回 null。</summary>
    public async Task<CircleInvitation?> GetByCodeAsync(string code)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.Code == code);
    }

    /// <summary>按令牌获取邀请记录，不存在时返回 null。</summary>
    public async Task<CircleInvitation?> GetByTokenAsync(Guid token)
    {
        return await DbSet.FirstOrDefaultAsync(i => i.Token == token);
    }

    /// <summary>分页获取圈子内有效（待处理且未过期）的邀请码记录，按创建时间倒序。</summary>
    public async Task<IEnumerable<CircleInvitation>> GetByCircleAsync(Guid circleGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var now = DateTimeOffset.UtcNow;
        return await DbSet
            .Where(i => i.CircleGuid == circleGuid
                        && i.Type == CircleInvitationType.Code
                        && i.Status == CircleInvitationStatus.Pending
                        && i.ExpireTime > now)
            .OrderByDescending(i => i.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>统计圈子内有效邀请码的数量。</summary>
    public async Task<int> GetCountByCircleAsync(Guid circleGuid)
    {
        var now = DateTimeOffset.UtcNow;
        return await DbSet.CountAsync(i => i.CircleGuid == circleGuid
                                           && i.Type == CircleInvitationType.Code
                                           && i.Status == CircleInvitationStatus.Pending
                                           && i.ExpireTime > now);
    }

    /// <summary>分页获取指定被邀请人的邀请记录，可选择仅返回待处理的记录，按创建时间倒序。</summary>
    public async Task<IEnumerable<CircleInvitation>> GetByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet.Where(i => i.InviteeGuid == inviteeGuid);
        if (pendingOnly)
            query = query.Where(i => i.Status == CircleInvitationStatus.Pending);

        return await query
            .OrderByDescending(i => i.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>统计指定被邀请人的邀请记录数量，可选择仅统计待处理的记录。</summary>
    public async Task<int> GetCountByInviteeAsync(Guid inviteeGuid, bool pendingOnly = true)
    {
        var query = DbSet.Where(i => i.InviteeGuid == inviteeGuid);
        if (pendingOnly)
            query = query.Where(i => i.Status == CircleInvitationStatus.Pending);
        return await query.CountAsync();
    }

    /// <summary>判断指定邀请码是否已存在。</summary>
    public async Task<bool> CodeExistsAsync(string code)
    {
        return await DbSet.AnyAsync(i => i.Code == code);
    }

    /// <summary>原子地将待处理且未过期的邀请更新为已接受，返回是否更新成功。</summary>
    public async Task<bool> TryAcceptAtomicallyAsync(Guid inviteGuid)
    {
        var now = DateTimeOffset.UtcNow;
        var rows = await DbSet
            .Where(i => i.InviteGuid == inviteGuid
                        && i.Status == CircleInvitationStatus.Pending
                        && i.ExpireTime > now)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, CircleInvitationStatus.Accepted));
        return rows > 0;
    }

    /// <summary>统计指定邀请人自某时间点以来创建的邀请码数量。</summary>
    public async Task<int> CountCodesCreatedSinceAsync(Guid inviterGuid, DateTimeOffset since)
    {
        return await DbSet.CountAsync(i => i.InviterGuid == inviterGuid
                                           && i.Type == CircleInvitationType.Code
                                           && i.CreateTime >= since);
    }

    /// <summary>新增圈子邀请并返回已跟踪的实体。</summary>
    public async Task<CircleInvitation> AddAsync(CircleInvitation invitation)
    {
        var entry = await DbSet.AddAsync(invitation);
        return entry.Entity;
    }

    /// <summary>更新圈子邀请并返回已跟踪的实体。</summary>
    public async Task<CircleInvitation> UpdateAsync(CircleInvitation invitation)
    {
        var entry = DbSet.Update(invitation);
        return entry.Entity;
    }

    /// <summary>删除指定邀请记录。</summary>
    public async Task DeleteAsync(Guid inviteGuid)
    {
        var invitation = await GetByIdAsync(inviteGuid);
        if (invitation is not null)
            DbSet.Remove(invitation);
    }
}
