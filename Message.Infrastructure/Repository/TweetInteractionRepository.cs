using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class TweetInteractionRepository : ITweetInteractionRepository
{
    private readonly MessageDbContext _context;
    private readonly DbSet<TweetInteraction> _dbSet;

    public TweetInteractionRepository(MessageDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TweetInteraction>();
    }

    public async Task<TweetInteraction?> GetAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        return await _dbSet.FirstOrDefaultAsync(i =>
            i.TweetGuid == tweetGuid && i.UserGuid == userGuid && i.Type == type);
    }

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

    public async Task<bool> ExistsAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        return await _dbSet.AnyAsync(i =>
            i.TweetGuid == tweetGuid && i.UserGuid == userGuid && i.Type == type);
    }

    public async Task<TweetInteraction> AddAsync(TweetInteraction interaction)
    {
        if (interaction == null) throw new ArgumentNullException(nameof(interaction));
        var entry = await _dbSet.AddAsync(interaction);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid tweetGuid, Guid userGuid, InteractionType type)
    {
        var interaction = await GetAsync(tweetGuid, userGuid, type);
        if (interaction is not null)
        {
            _dbSet.Remove(interaction);
        }
    }

    public async Task<int> GetCountByTweetAsync(Guid tweetGuid, InteractionType type)
    {
        return await _dbSet.CountAsync(i => i.TweetGuid == tweetGuid && i.Type == type);
    }
}
