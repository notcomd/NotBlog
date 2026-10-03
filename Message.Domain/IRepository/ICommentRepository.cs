
namespace Message.Domain.IRepository;

/// <summary>
/// 评论仓储接口（Comment 聚合根）。
/// </summary>
public interface ICommentRepository : IRepository<Comment, IUnitOfWork>
{
    /// <summary>按评论 ID 查询（不存在返回 null）</summary>
    Task<Comment?> GetByIdAsync(Guid commentGuid);
    /// <summary>分页获取某推文的评论（按时间倒序）</summary>
    Task<IEnumerable<Comment>> GetByTweetAsync(Guid tweetGuid, int page = 1, int pageSize = 20);
    /// <summary>分页获取某评论的回复</summary>
    Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentGuid, int page = 1, int pageSize = 10);
    /// <summary>新增评论</summary>
    Task<Comment> AddAsync(Comment comment);
    /// <summary>更新评论</summary>
    Task<Comment> UpdateAsync(Comment comment);
    /// <summary>删除评论</summary>
    Task DeleteAsync(Guid commentGuid);
    /// <summary>判断评论是否存在</summary>
    Task<bool> ExistsAsync(Guid commentGuid);
    /// <summary>统计某推文的评论数量</summary>
    Task<int> GetCountByTweetAsync(Guid tweetGuid);
    /// <summary>统计某评论的回复数量</summary>
    Task<int> GetReplyCountAsync(Guid commentGuid);
}
