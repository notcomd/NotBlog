using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频到收藏夹命令处理器。
/// </summary>
public class AddVideoToCollectionCommandHandler(
    IVideoCollectionRepository collectionRepository,
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

        logger.LogInformation("Video {VideoGuid} added to collection {CollectionGuid}",
            request.VideoGuid, request.CollectionGuid);

        return new CollectionOperationResult(true, null);
    }
}

/// <summary>
/// 从收藏夹移除视频命令处理器。
/// </summary>
public class RemoveVideoFromCollectionCommandHandler(
    IVideoCollectionRepository collectionRepository,
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

        logger.LogInformation("Video {VideoGuid} removed from collection {CollectionGuid}",
            request.VideoGuid, request.CollectionGuid);

        return new CollectionOperationResult(true, null);
    }
}
