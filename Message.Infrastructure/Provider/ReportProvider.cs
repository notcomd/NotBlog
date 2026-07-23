namespace Message.Infrastructure.Provider;

public class ReportProvider : IReportProvider
{
    private readonly ITweetReportRepository _reportRepository;
    private readonly ITweetRepository _tweetRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReportProvider> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public ReportProvider(
        ITweetReportRepository reportRepository,
        ITweetRepository tweetRepository,
        ICommentRepository commentRepository,
        ICurrentUserService currentUserService,
        ILogger<ReportProvider> logger,
        IUnitOfWork unitOfWork)
    {
        _reportRepository = reportRepository;
        _tweetRepository = tweetRepository;
        _commentRepository = commentRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<TweetReport> SubmitReportAsync(Guid reporterGuid, string targetType, Guid targetGuid,
        string reason, string category, IEnumerable<string>? evidenceUrls = null)
    {
        try
        {
            var parsedTargetType = Enum.Parse<ReportTargetType>(targetType);
            var parsedCategory = Enum.Parse<ReportCategory>(category);

            _logger.LogInformation("开始提交举报，举报人: {ReporterGuid}, 类型: {TargetType}, 目标: {TargetGuid}",
                reporterGuid, parsedTargetType, targetGuid);

            // 验证目标存在并获取被举报用户ID
            Guid reportedUserGuid;
            if (parsedTargetType == ReportTargetType.Tweet)
            {
                var tweet = await _tweetRepository.GetByIdAsync(targetGuid);
                if (tweet == null)
                    throw new KeyNotFoundException("被举报的推文不存在");
                reportedUserGuid = tweet.AuthorGuid;
            }
            else if (parsedTargetType == ReportTargetType.Comment)
            {
                var comment = await _commentRepository.GetByIdAsync(targetGuid);
                if (comment == null)
                    throw new KeyNotFoundException("被举报的评论不存在");
                reportedUserGuid = comment.UserGuid;
            }
            else
            {
                throw new ArgumentException("不支持的举报目标类型");
            }

            var report = TweetReport.Create(reporterGuid, parsedTargetType, targetGuid, reportedUserGuid,
                reason, parsedCategory, evidenceUrls);

            await _reportRepository.AddAsync(report);
            await _unitOfWork.SaveEntitiesAsync();

            _logger.LogInformation("举报提交成功，ID: {ReportGuid}", report.ReportGuid);
            return report;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException and not ArgumentException)
        {
            _logger.LogError(ex, "提交举报失败，举报人: {ReporterGuid}", reporterGuid);
            throw;
        }
    }

    public async Task<IEnumerable<TweetReport>> GetMyReportsAsync(Guid reporterGuid, int page = 1, int pageSize = 20)
    {
        try
        {
            return await _reportRepository.GetByReporterAsync(reporterGuid, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取我的举报列表失败，用户: {ReporterGuid}", reporterGuid);
            throw;
        }
    }
}
