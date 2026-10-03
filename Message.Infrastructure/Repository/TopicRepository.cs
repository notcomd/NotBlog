
namespace Message.Infrastructure.Repository;

/// <summary>话题仓储实现，负责 Topics 表的查询与持久化。</summary>
public class TopicRepository(MessageDbContext context) : ITopicRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Topic> DbSet = context.Topics;

    /// <summary>按话题标识获取话题，不存在时返回 null。</summary>
    public async Task<Topic?> GetByIdAsync(Guid topicGuid)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.TopicGuid == topicGuid);
    }

    /// <summary>按名称获取话题，不存在时返回 null。</summary>
    public async Task<Topic?> GetByNameAsync(string name)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.Name == name);
    }

    /// <summary>分页获取启用中的话题，按帖子数与创建时间倒序。</summary>
    public async Task<IEnumerable<Topic>> GetActiveAsync(int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        return await DbSet
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.PostCount)
            .ThenByDescending(t => t.CreateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>统计启用中的话题数量。</summary>
    public async Task<int> GetActiveCountAsync()
    {
        return await DbSet.CountAsync(t => t.IsActive);
    }

    /// <summary>获取指定 ID 集合中已存在的启用话题。</summary>
    public async Task<IEnumerable<Topic>> GetExistingActiveAsync(IEnumerable<Guid> topicGuids)
    {
        var ids = (topicGuids ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await DbSet
            .Where(t => t.IsActive && ids.Contains(t.TopicGuid))
            .ToListAsync();
    }

    /// <summary>新增话题并返回已跟踪的实体。</summary>
    public async Task<Topic> AddAsync(Topic topic)
    {
        var entry = await DbSet.AddAsync(topic);
        return entry.Entity;
    }

    /// <summary>更新话题并返回已跟踪的实体。</summary>
    public async Task<Topic> UpdateAsync(Topic topic)
    {
        var entry = DbSet.Update(topic);
        return entry.Entity;
    }

    /// <summary>判断指定话题是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid topicGuid)
    {
        return await DbSet.AnyAsync(t => t.TopicGuid == topicGuid);
    }

    /// <summary>判断指定名称的话题是否已存在。</summary>
    public async Task<bool> NameExistsAsync(string name)
    {
        return await DbSet.AnyAsync(t => t.Name == name);
    }
}
