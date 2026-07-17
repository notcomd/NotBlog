using Message.Domain.Entities.Tweet;

namespace Message.Domain.IRepository;

public interface ICommentRepository
{
    Task<Comment?> GetByIdAsync(Guid commentGuid);
    Task<IEnumerable<Comment>> GetByTweetAsync(Guid tweetGuid, int page = 1, int pageSize = 20);
    Task<IEnumerable<Comment>> GetRepliesAsync(Guid parentGuid, int page = 1, int pageSize = 10);
    Task<Comment> AddAsync(Comment comment);
    Task<Comment> UpdateAsync(Comment comment);
    Task DeleteAsync(Guid commentGuid);
    Task<bool> ExistsAsync(Guid commentGuid);
    Task<int> GetCountByTweetAsync(Guid tweetGuid);
    Task<int> GetReplyCountAsync(Guid commentGuid);
}
