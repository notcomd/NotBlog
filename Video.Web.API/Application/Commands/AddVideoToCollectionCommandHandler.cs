using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频到收藏夹命令处理器：收藏关系写入的同时，视频收藏计数（VideoQuote.Stars）＋1。
/// 重复加入返回幂等成功，不重复计数。
/// </summary>
public class AddVideoToCollectionCommandHandler(
    IVideoCollectionRepository collectionRepository,
    IVideoRepository videoRepository,
    ILogger<AddVideoToCollectionCommandHandler> logger)
    : IRequestHandler<AddVideoToCollectionCommand, CollectionOperationResult>
{
    public async Task<CollectionOperationResult> Handler(AddVideoToCollectionCommand request,
        CancellationToken cancellationToken)
    {
        VideoCollection collection;
        try
        {
            collection = await collectionRepository.FindByVideoCollectionAsync(request.CollectionGuid);
        }
        catch
        {
            return new CollectionOperationResult(false, "Collection not found.");
        }

        // 归属校验（S-07）：仅收藏夹所属用户可向其收藏夹添加视频，禁止操作他人收藏夹
        if (collection.AffiliatedUser is null || !collection.AffiliatedUser.Contains(request.UserGuid))
            return new CollectionOperationResult(false, "无权操作该收藏夹");

        if (collection.VideoGuid.Contains(request.VideoGuid))
            return new CollectionOperationResult(true, "Video already in collection."); // 幂等

        collection.VideoGuid.Add(request.VideoGuid);
        await collectionRepository.UpdateByVideoCollectionAsync(collection);

        // 收藏计数 +1（互动实施文档 §6.1；计数失败不影响收藏关系已提交，记录日志兜底）
        try
        {
            var video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            video.VideoQuote.UpStars();
            await videoRepository.UpdateByQuoteAsync(request.VideoGuid, video.VideoQuote);
            logger.LogInformation("Video {VideoGuid} stars count incremented by collection add", request.VideoGuid);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "收藏计数 +1 失败，收藏关系已提交：Video={VideoGuid}", request.VideoGuid);
        }

        logger.LogInformation("Video {VideoGuid} added to collection {CollectionGuid}",
            request.VideoGuid, request.CollectionGuid);

        return new CollectionOperationResult(true, null);
    }
}

/// <summary>
/// 从收藏夹移除视频命令处理器：收藏关系删除的同时，视频收藏计数（VideoQuote.Stars）−1。
/// 未在收藏夹中返回幂等成功，不扣减计数。
/// </summary>
public class RemoveVideoFromCollectionCommandHandler(
    IVideoCollectionRepository collectionRepository,
    IVideoRepository videoRepository,
    ILogger<RemoveVideoFromCollectionCommandHandler> logger)
    : IRequestHandler<RemoveVideoFromCollectionCommand, CollectionOperationResult>
{
    public async Task<CollectionOperationResult> Handler(RemoveVideoFromCollectionCommand request,
        CancellationToken cancellationToken)
    {
        VideoCollection collection;
        try
        {
            collection = await collectionRepository.FindByVideoCollectionAsync(request.CollectionGuid);
        }
        catch
        {
            return new CollectionOperationResult(false, "Collection not found.");
        }

        // 归属校验（S-07）：仅收藏夹所属用户可从收藏夹移除视频
        if (collection.AffiliatedUser is null || !collection.AffiliatedUser.Contains(request.UserGuid))
            return new CollectionOperationResult(false, "无权操作该收藏夹");

        if (!collection.VideoGuid.Contains(request.VideoGuid))
            return new CollectionOperationResult(true, "Video not in collection."); // 幂等

        collection.VideoGuid.Remove(request.VideoGuid);
        await collectionRepository.UpdateByVideoCollectionAsync(collection);

        // 收藏计数 −1（下限 0 由 VideoQuote 原子减保护）
        try
        {
            var video = await videoRepository.FindByVideoAsync(request.VideoGuid);
            video.VideoQuote.DownStars();
            await videoRepository.UpdateByQuoteAsync(request.VideoGuid, video.VideoQuote);
            logger.LogInformation("Video {VideoGuid} stars count decremented by collection remove", request.VideoGuid);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "收藏计数 −1 失败，收藏关系已提交：Video={VideoGuid}", request.VideoGuid);
        }

        logger.LogInformation("Video {VideoGuid} removed from collection {CollectionGuid}",
            request.VideoGuid, request.CollectionGuid);

        return new CollectionOperationResult(true, null);
    }
}
