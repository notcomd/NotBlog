using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 视频点赞命令处理器 — 支持视频 upvote/down/ballot/share 操作。
/// </summary>
public class LikeVideoCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<LikeVideoCommandHandler> logger)
    : IRequestHandler<LikeVideoCommand, LikeVideoResult>
{
    public async Task<LikeVideoResult> Handler(LikeVideoCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.Field.ToLowerInvariant();
        var validFields = new HashSet<string> { "upvote", "down", "ballot", "share" };

        if (!validFields.Contains(normalized))
            return new LikeVideoResult(false, 0, $"Invalid field '{request.Field}'. Valid: upvote, down, ballot, share.");

        // 写路径必须从仓储加载实体，确保被 DbContext 跟踪后修改可落库
        // （缓存反序列化出的实体未被跟踪，直接 SaveChanges 会静默丢失）
        var video = await videoRepository.FindByVideoWithDetailsAsync(request.VideoGuid);

        var quote = video.VideoQuote;
        if (request.IsLike)
        {
            switch (normalized)
            {
                case "upvote": quote.UpUpvote(); break;
                case "down": quote.UpDown(); break;
                case "ballot": quote.UpBallot(); break;
                case "share": quote.UpShare(); break;
            }
        }
        else
        {
            switch (normalized)
            {
                case "upvote": quote.DownUpvote(); break;
                case "down": quote.DownDown(); break;
                case "ballot": quote.DownBallot(); break;
                case "share": quote.DownShare(); break;
            }
        }

        await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
        await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);
        await cacheService.InvalidateVideoListsAsync(cancellationToken);

        var newCount = normalized switch
        {
            "upvote" => quote.Upvote,
            "down" => quote.Down,
            "ballot" => quote.Ballot,
            "share" => quote.Share,
            _ => 0L
        };

        logger.LogInformation("Video like: {VideoGuid} {Field} IsLike={IsLike} NewCount={Count}",
            request.VideoGuid, request.Field, request.IsLike, newCount);

        return new LikeVideoResult(true, newCount, null);
    }
}
