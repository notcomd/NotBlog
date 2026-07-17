using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Domain.SeedWork;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Provider;

public class CommentProvider : ICommentProvider
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITweetRepository _tweetRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CommentProvider> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CommentProvider(
        ICommentRepository commentRepository,
        ITweetRepository tweetRepository,
        ICurrentUserService currentUserService,
        ILogger<CommentProvider> logger,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _tweetRepository = tweetRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Comment> AddCommentAsync(Guid tweetGuid, Guid userGuid, string content,
        Guid? parentGuid = null, Guid? replyToGuid = null)
    {
        try
        {
            _logger.LogInformation("开始添加评论，推文: {TweetGuid}, 用户: {UserGuid}", tweetGuid, userGuid);

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            if (tweet.TweetStatus != TweetStatus.Approved)
                throw new InvalidOperationException("只有已审核通过的推文才能评论");

            // 检查回复嵌套层级（最多2层）
            if (parentGuid.HasValue)
            {
                var parentComment = await _commentRepository.GetByIdAsync(parentGuid.Value);
                if (parentComment == null)
                    throw new KeyNotFoundException("父评论不存在");

                if (parentComment.ParentGuid.HasValue)
                    throw new InvalidOperationException("评论嵌套层级不能超过2层");
            }

            var comment = Comment.Create(tweetGuid, userGuid, content, parentGuid, replyToGuid);
            await _commentRepository.AddAsync(comment);

            tweet.AddComment();
            await _tweetRepository.UpdateAsync(tweet);

            if (parentGuid.HasValue)
            {
                var parentComment = await _commentRepository.GetByIdAsync(parentGuid.Value);
                if (parentComment != null)
                {
                    parentComment.IncrementReplyCount();
                    await _commentRepository.UpdateAsync(parentComment);
                }
            }

            await _unitOfWork.SavaEntitiesAsync();

            _logger.LogInformation("评论添加成功，ID: {CommentGuid}", comment.CommentGuid);
            return comment;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not InvalidOperationException)
        {
            _logger.LogError(ex, "添加评论失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<IEnumerable<Comment>> GetTweetCommentsAsync(Guid tweetGuid, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _commentRepository.GetByTweetAsync(tweetGuid, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取推文评论失败，推文: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<IEnumerable<Comment>> GetCommentRepliesAsync(Guid commentGuid, int page = 1, int pageSize = 10)
    {
        try
        {
            return await _commentRepository.GetRepliesAsync(commentGuid, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论回复失败，评论: {CommentGuid}", commentGuid);
            throw;
        }
    }

    public async Task DeleteCommentAsync(Guid commentGuid, Guid userGuid)
    {
        try
        {
            _logger.LogInformation("开始删除评论，ID: {CommentGuid}", commentGuid);

            var comment = await _commentRepository.GetByIdAsync(commentGuid);
            if (comment == null)
                throw new KeyNotFoundException("评论不存在");

            if (comment.UserGuid != userGuid && !_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("无权删除此评论");

            await _commentRepository.DeleteAsync(commentGuid);
            await _unitOfWork.SavaEntitiesAsync();

            _logger.LogInformation("评论删除成功，ID: {CommentGuid}", commentGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "删除评论失败，ID: {CommentGuid}", commentGuid);
            throw;
        }
    }
}
