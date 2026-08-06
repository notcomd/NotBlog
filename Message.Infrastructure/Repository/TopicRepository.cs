
namespace Message.Infrastructure.Repository;

public class TopicRepository(MessageDbContext context) : ITopicRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Topic> DbSet = context.Topics;

    public async Task<Topic?> GetByIdAsync(Guid topicGuid)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.TopicGuid == topicGuid);
    }

    public async Task<Topic?> GetByNameAsync(string name)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.Name == name);
    }

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

    public async Task<int> GetActiveCountAsync()
    {
        return await DbSet.CountAsync(t => t.IsActive);
    }

    public async Task<IEnumerable<Topic>> GetExistingActiveAsync(IEnumerable<Guid> topicGuids)
    {
        var ids = (topicGuids ?? []).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        return await DbSet
            .Where(t => t.IsActive && ids.Contains(t.TopicGuid))
            .ToListAsync();
    }

    public async Task<Topic> AddAsync(Topic topic)
    {
        var entry = await DbSet.AddAsync(topic);
        return entry.Entity;
    }

    public async Task<Topic> UpdateAsync(Topic topic)
    {
        var entry = DbSet.Update(topic);
        return entry.Entity;
    }

    public async Task<bool> ExistsAsync(Guid topicGuid)
    {
        return await DbSet.AnyAsync(t => t.TopicGuid == topicGuid);
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        return await DbSet.AnyAsync(t => t.Name == name);
    }
}
