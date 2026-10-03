
namespace Message.Infrastructure.Repository;

/// <summary>评论仓储实现，负责 Comments 表的查询与持久化。</summary>
public class CommentRepository(MessageDbContext context) : ICommentRepository
{
    /// <summary>获取当前数据库上下文作为工作单元。</summary>
    public IUnitOfWork UnitOfWork => context;

    private readonly DbSet<Comment> DbSet = context.Comments;

    /// <summary>按评论标识获取评论，不存在时返回 null。</summary>
    public async Task<Comment?> GetByIdAsync(Guid commentGuid)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.CommentGuid == commentGuid);
    }

    /// <summary>分页获取动态下的顶级评论（未删除），按创建时间升序。</summary>
    public async Task<IEnumerable<Comment>> GetByTweetAsync(Guid tweetGuid, int page = 1, int pageSize = 20)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Where(c => c.TweetGuid == tweetGuid && !c.IsDeleted && c.ParentGuid == null)
            .OrderBy(c => c.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>分页获取指定父评论的回复（未删除），按创建时间升序。</summary>
    public async Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentGuid, int page = 1, int pageSize = 10)
    {
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = DbSet
            .Where(c => c.ParentGuid == parentGuid && !c.IsDeleted)
            .OrderBy(c => c.CreateTime);

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    /// <summary>新增评论并返回已跟踪的实体。</summary>
    public async Task<Comment> AddAsync(Comment comment)
    {
        var entry = await DbSet.AddAsync(comment);
        return entry.Entity;
    }

    /// <summary>更新评论并返回已跟踪的实体。</summary>
    public async Task<Comment> UpdateAsync(Comment comment)
    {
        var entry = DbSet.Update(comment);
        return entry.Entity;
    }

    /// <summary>软删除指定评论。</summary>
    public async Task DeleteAsync(Guid commentGuid)
    {
        var comment = await GetByIdAsync(commentGuid);
        if (comment is not null)
        {
            comment.SoftDelete();
            DbSet.Update(comment);
        }
    }

    /// <summary>判断指定未删除的评论是否存在。</summary>
    public async Task<bool> ExistsAsync(Guid commentGuid)
    {
        return await DbSet.AnyAsync(c => c.CommentGuid == commentGuid && !c.IsDeleted);
    }

    /// <summary>统计指定动态下未删除的评论数量。</summary>
    public async Task<int> GetCountByTweetAsync(Guid tweetGuid)
    {
        return await DbSet.CountAsync(c => c.TweetGuid == tweetGuid && !c.IsDeleted);
    }

    /// <summary>统计指定评论下未删除的回复数量。</summary>
    public async Task<int> GetReplyCountAsync(Guid commentGuid)
    {
        return await DbSet.CountAsync(c => c.ParentGuid == commentGuid && !c.IsDeleted);
    }
}
