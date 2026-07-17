using System.Runtime.CompilerServices;
using Message.Domain.Dto;
using Message.Domain.Entities.Tweet;
using Message.Domain.Enums;
using Message.Domain.IProvider;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Domain.SeedWork;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Provider;

public class AuditProvider(
    ITweetRepository tweetRepository,
    ITweetAuditRepository auditRepository,
    ITweetReportRepository reportRepository,
    ICommentRepository commentRepository,
    ICurrentUserService currentUserService,
    ILogger<AuditProvider> logger,
    IUnitOfWork unitOfWork) : IAuditProvider
{
    private readonly ITweetRepository _tweetRepository = tweetRepository;
    private readonly ITweetAuditRepository _auditRepository = auditRepository;
    private readonly ITweetReportRepository _reportRepository = reportRepository;
    private readonly ICommentRepository _commentRepository = commentRepository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<AuditProvider> _logger = logger;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;


    /// <summary>
    /// 获取待审核推文列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>待审核推文列表</returns>
    public async Task<IEnumerable<Tweet>> GetPendingTweetsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await _tweetRepository.GetByStatusAsync(TweetStatus.Pending, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待审核推文列表失败");
            throw;
        }
    }

    /// <summary>
    /// 审核通过推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="auditorGuid">审核员ID</param>
    public async Task ApproveTweetAsync(Guid tweetGuid, Guid auditorGuid)
    {
        try
        {
            _logger.LogInformation("开始审核通过推文，ID: {TweetGuid}, 审核员: {AuditorGuid}", tweetGuid, auditorGuid);

            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能审核推文");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            tweet.Approve(auditorGuid);
            await _tweetRepository.UpdateAsync(tweet);

            var auditLog = TweetAuditLog.Create(tweetGuid, auditorGuid, AuditAction.Approve, "审核通过");
            await _auditRepository.AddAsync(auditLog);

            await _unitOfWork.SavaEntitiesAsync();

            _logger.LogInformation("推文审核通过成功，ID: {TweetGuid}", tweetGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "审核通过推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    /// <summary>
    /// 驳回推文
    /// </summary>
    /// <param name="tweetGuid">推文ID</param>
    /// <param name="auditorGuid">审核员ID</param>
    /// <param name="reason">驳回原因</param>
    public async Task RejectTweetAsync(Guid tweetGuid, Guid auditorGuid, string reason)
    {
        try
        {
            _logger.LogInformation("开始驳回推文，ID: {TweetGuid}, 审核员: {AuditorGuid}, 原因: {Reason}",
                tweetGuid, auditorGuid, reason);

            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能审核推文");

            var tweet = await _tweetRepository.GetByIdAsync(tweetGuid);
            if (tweet == null)
                throw new KeyNotFoundException("推文不存在");

            tweet.Reject(auditorGuid, reason);
            await _tweetRepository.UpdateAsync(tweet);

            var auditLog = TweetAuditLog.Create(tweetGuid, auditorGuid, AuditAction.Reject, reason);
            await _auditRepository.AddAsync(auditLog);

            await _unitOfWork.SavaEntitiesAsync();

            _logger.LogInformation("推文驳回成功，ID: {TweetGuid}", tweetGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "驳回推文失败，ID: {TweetGuid}", tweetGuid);
            throw;
        }
    }

    public async Task<IEnumerable<TweetReport>> GetPendingReportsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            return await _reportRepository.GetByStatusAsync(ReportStatus.Pending, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待处理举报列表失败");
            throw;
        }
    }

    public async Task ResolveReportAsync(Guid reportGuid, Guid reviewerGuid, string note, bool isContentRemoved)
    {
        try
        {
            _logger.LogInformation("开始处理举报，ID: {ReportGuid}, 审核员: {ReviewerGuid}", reportGuid, reviewerGuid);

            if (!_currentUserService.IsAdmin())
                throw new UnauthorizedAccessException("只有管理员才能处理举报");

            var report = await _reportRepository.GetByIdAsync(reportGuid);
            if (report == null)
                throw new KeyNotFoundException("举报不存在");

            report.StartReview(reviewerGuid);

            if (isContentRemoved)
            {
                report.ResolveRemoved(reviewerGuid, note);

                // 删除被举报的内容
                if (report.TargetType == ReportTargetType.Tweet)
                {
                    var tweet = await _tweetRepository.GetByIdAsync(report.TargetGuid);
                    if (tweet != null)
                    {
                        await _tweetRepository.DeleteAsync(report.TargetGuid);
                        _logger.LogInformation("已删除被举报推文，ID: {TargetGuid}", report.TargetGuid);
                    }
                }
                else if (report.TargetType == ReportTargetType.Comment)
                {
                    var comment = await _commentRepository.GetByIdAsync(report.TargetGuid);
                    if (comment != null && !comment.IsDeleted)
                    {
                        await _commentRepository.DeleteAsync(report.TargetGuid);
                        _logger.LogInformation("已删除被举报评论，ID: {TargetGuid}", report.TargetGuid);
                    }
                }
            }
            else
            {
                report.ResolveRejected(reviewerGuid, note);
            }

            await _reportRepository.UpdateAsync(report);
            await _unitOfWork.SavaEntitiesAsync();

            _logger.LogInformation("举报处理成功，ID: {ReportGuid}", reportGuid);
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "处理举报失败，ID: {ReportGuid}", reportGuid);
            throw;
        }
    }


}
