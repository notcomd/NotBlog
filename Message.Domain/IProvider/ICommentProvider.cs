using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;

namespace Message.Domain.IProvider;

public interface ICommentProvider
{
    Task<Comment> AddCommentAsync(Guid tweetGuid, Guid userGuid, string content,
        Guid? parentGuid = null, Guid? replyToGuid = null);

    Task<IEnumerable<Comment>> GetTweetCommentsAsync(Guid tweetGuid, int page = 1, int pageSize = 20);

    Task<IEnumerable<Comment>> GetCommentRepliesAsync(Guid commentGuid, int page = 1, int pageSize = 10);

    Task DeleteCommentAsync(Guid commentGuid, Guid userGuid);
}
