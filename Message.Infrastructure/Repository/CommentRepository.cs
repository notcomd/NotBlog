using Message.Infrastructure.EntityFramework;

namespace Message.Infrastructure.Repository;

public class CommentRepository(MessageDbContext context) : ICommentRepository
{
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Comment> DbSet = context.Comments;

    public async Task<Comment?> GetByIdAsync(Guid commentGuid)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.CommentGuid == commentGuid);
    }

    public async Task<IEnumerable<Comment>> GetByTweetAsync(Guid tweetGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Where(c => c.TweetGuid == tweetGuid && !c.IsDeleted && c.ParentGuid == null)
            .OrderBy(c => c.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentGuid, int page = 1, int pageSize = 10)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Where(c => c.ParentGuid == parentGuid && !c.IsDeleted)
            .OrderBy(c => c.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<Comment> AddAsync(Comment comment)
    {
        var entry = await DbSet.AddAsync(comment);
        return entry.Entity;
    }

    public async Task<Comment> UpdateAsync(Comment comment)
    {
        var entry = DbSet.Update(comment);
        return entry.Entity;
    }

    public async Task DeleteAsync(Guid commentGuid)
    {
        var comment = await GetByIdAsync(commentGuid);
        if (comment is not null)
        {
            comment.SoftDelete();
            DbSet.Update(comment);
        }
    }

    public async Task<bool> ExistsAsync(Guid commentGuid)
    {
        return await DbSet.AnyAsync(c => c.CommentGuid == commentGuid && !c.IsDeleted);
    }

    public async Task<int> GetCountByTweetAsync(Guid tweetGuid)
    {
        return await DbSet.CountAsync(c => c.TweetGuid == tweetGuid && !c.IsDeleted);
    }

    public async Task<int> GetReplyCountAsync(Guid commentGuid)
    {
        return await DbSet.CountAsync(c => c.ParentGuid == commentGuid && !c.IsDeleted);
    }
}
