using Message.Domain.Entities.Tweet;
using Message.Domain.IRepository;
using Message.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Message.Infrastructure.Repository;

public class CommentRepository(MessageDbContext context) : ICommentRepository
{
    
    private readonly DbSet<Comment> DbSet = context.Comments;

    public async Task<Comment?> GetByIdAsync(Guid commentGuid)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.CommentGuid == commentGuid);
    }

    public async Task<IEnumerable<Comment>> GetByTweetAsync(Guid tweetGuid, int page = 1, int pageSize = 20)
    {
        var query = DbSet
            .Where(c => c.TweetGuid == tweetGuid && !c.IsDeleted && c.ParentGuid == null)
            .OrderBy(c => c.CreateTime);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentGuid, int page = 1, int pageSize = 10)
    {
        var query = DbSet
            .Where(c => c.ParentGuid == parentGuid && !c.IsDeleted)
            .OrderBy(c => c.CreateTime);

        return await query.ToListAsync();
    }

    public new async Task<Comment> AddAsync(Comment comment)
    {
        var entry = await DbSet.AddAsync(comment);
        return entry.Entity;
    }

    public new async Task<Comment> UpdateAsync(Comment comment)
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
