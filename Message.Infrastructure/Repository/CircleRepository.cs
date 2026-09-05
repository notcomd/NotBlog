
namespace Message.Infrastructure.Repository;

public class CircleRepository(MessageDbContext context) : ICircleRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Circle> DbSet = context.Circles;

    public async Task<Circle?> GetByIdAsync(Guid circleGuid)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.CircleGuid == circleGuid && c.Status == CircleStatus.Active);
    }

    public async Task<Circle?> GetByIdWithMembersAsync(Guid circleGuid)
    {
        return await DbSet
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.CircleGuid == circleGuid && c.Status == CircleStatus.Active);
    }

    public async Task<IEnumerable<Circle>> GetByMemberAsync(Guid userId)
    {
        return await context.CircleMembers
            .Where(m => m.UserGuid == userId && m.Status == CircleMemberStatus.Active)
            .Join(DbSet,
                m => m.CircleGuid,
                c => c.CircleGuid,
                (m, c) => c)
            .Where(c => c.Status == CircleStatus.Active)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Circle>> GetByOwnerAsync(Guid ownerGuid)
    {
        return await DbSet
            .Where(c => c.OwnerGuid == ownerGuid && c.Status == CircleStatus.Active)
            .OrderByDescending(c => c.CreateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Circle>> GetActiveAsync(string? keyword, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var query = DbSet.Where(c => c.Status == CircleStatus.Active);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{EscapeLike(keyword.Trim())}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern));
        }

        return await query
            .OrderByDescending(c => c.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetActiveCountAsync(string? keyword)
    {
        var query = DbSet.Where(c => c.Status == CircleStatus.Active);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{EscapeLike(keyword.Trim())}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern));
        }
        return await query.CountAsync();
    }

    /// <summary>LIKE 模式转义（% _ \ 为通配/转义符，按原义匹配）</summary>
    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<bool> IsMemberAsync(Guid circleGuid, Guid userId)
    {
        return await context.CircleMembers.AnyAsync(
            m => m.CircleGuid == circleGuid && m.UserGuid == userId && m.Status == CircleMemberStatus.Active);
    }

    public async Task<CircleMember?> GetMemberAsync(Guid circleGuid, Guid userId)
    {
        return await context.CircleMembers
            .FirstOrDefaultAsync(m => m.CircleGuid == circleGuid && m.UserGuid == userId);
    }

    public async Task<IEnumerable<CircleMember>> GetMembersAsync(Guid circleGuid, int page = 1, int pageSize = 50)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await context.CircleMembers
            .Where(m => m.CircleGuid == circleGuid && m.Status == CircleMemberStatus.Active)
            .OrderBy(m => m.JoinTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetMemberCountAsync(Guid circleGuid)
    {
        return await context.CircleMembers.CountAsync(
            m => m.CircleGuid == circleGuid && m.Status == CircleMemberStatus.Active);
    }

    public async Task<Circle> AddAsync(Circle circle)
    {
        var entry = await DbSet.AddAsync(circle);
        return entry.Entity;
    }

    public async Task<Circle> UpdateAsync(Circle circle)
    {
        // 仅对未跟踪实体执行 DbSet.Update；已跟踪实体交由 ChangeTracker 自动检测修改。
        // 修复（2026-08-15）：DbSet.Update 会递归遍历对象图，把聚合内「新增」子实体（如
        // AddMember 新建的 CircleMember）从 Added 强制改为 Modified，SaveChanges 时对
        // 不存在的行生成 UPDATE → 影响 0 行 → DbUpdateConcurrencyException（加入圈子失败）。
        if (context.Entry(circle).State == EntityState.Detached)
            DbSet.Update(circle);
        return circle;
    }

    public async Task DeleteAsync(Guid circleGuid)
    {
        var circle = await GetByIdAsync(circleGuid);
        if (circle is not null)
            DbSet.Remove(circle);
    }

    public async Task<bool> ExistsAsync(Guid circleGuid)
    {
        return await DbSet.AnyAsync(c => c.CircleGuid == circleGuid && c.Status == CircleStatus.Active);
    }

    /// <summary>管理端全量圈子分页（含已解散，keyword 模糊匹配名称，按创建时间倒序）</summary>
    public async Task<IEnumerable<Circle>> GetPagedAsync(string? keyword, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{EscapeLike(keyword.Trim())}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern));
        }

        return await query
            .OrderByDescending(c => c.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>管理端全量圈子总数（含已解散，与 GetPagedAsync 同条件）</summary>
    public async Task<int> GetTotalCountAsync(string? keyword)
    {
        var query = DbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{EscapeLike(keyword.Trim())}%";
            query = query.Where(c => EF.Functions.Like(c.Name, pattern));
        }
        return await query.CountAsync();
    }
}
