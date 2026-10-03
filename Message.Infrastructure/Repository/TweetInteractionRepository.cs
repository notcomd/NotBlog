
namespace Message.Infrastructure.Repository;

/// <summary>动态互动仓储实现，负责 TweetInteractions（点赞、收藏等互动）的查询与持久化。</summary>
public class TweetInteractionRepository : ITweetInteractionRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetInteraction> _dbSet;

    /// <summary>初始化 <see cref="TweetInteractionRepository"/> 实例。</summary>
    /// <param name="context">数据库上下文。</param>
    public TweetInteractionRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetInteraction>();
    }

    /// <summary>获取指定的互动记录，不存在时返回 null。</summary>
    public async Task<TweetInteraction?> GetAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        return await _dbSet.FirstOrDefaultAsync(i =>
            i.TweetGuid == tweetGuid && i.UserGuid == userGuid && i.Type == type);
    }

    /// <summary>分页获取指定动态的互动记录（可按类型过滤），按创建时间倒序。</summary>
    public async Task<IEnumerable<TweetInteraction>> GetByTweetAsync(Guid tweetGuid, InteractionType? type = null, int page = 1, int pageSize = 50)
    {
        var query = _dbSet.Where(i => i.TweetGuid == tweetGuid);

        if (type.HasValue)
            query = query.Where(i => i.Type == type.Value);

        query = query.OrderByDescending(i => i.CreateTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>分页获取指定用户的互动记录（可按类型过滤），按创建时间倒序。</summary>
    public async Task<IEnumerable<TweetInteraction>> GetByUserAsync(Guid userGuid, InteractionType? type = null, int page = 1, int pageSize = 20)
    {
        var query = _dbSet.Where(i => i.UserGuid == userGuid);

        if (type.HasValue)
            query = query.Where(i => i.Type == type.Value);

        query = query.OrderByDescending(i => i.CreateTime);

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>判断指定的互动记录是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        return await _dbSet.AnyAsync(i =>
            i.TweetGuid == tweetGuid && i.UserGuid == userGuid && i.Type == type);
    }

    /// <summary>新增互动记录并返回已跟踪的实体。</summary>
    public async Task<TweetInteraction> AddAsync(TweetInteraction interaction)
    {
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        var entry = await _dbSet.AddAsync(interaction);
        return entry.Entity;
    }

    /// <summary>删除指定的互动记录。</summary>
    public async Task DeleteAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        var interaction = await GetAsync(tweetGuid, userGuid, type);
        if (interaction is not null)
        {
            _dbSet.Remove(interaction);
        }
    }

    /// <summary>统计指定动态指定类型的互动数量。</summary>
    public async Task<int> GetCountByTweetAsync(Guid tweetGuid, InteractionType type)
    {
        return await _dbSet.CountAsync(i => i.TweetGuid == tweetGuid && i.Type == type);
    }

    /// <summary>统计指定用户指定类型的互动数量。</summary>
    public async Task<int> GetCountByUserAsync(Guid userGuid, InteractionType type)
    {
        return await _dbSet.CountAsync(i => i.UserGuid == userGuid && i.Type == type);
    }
}
