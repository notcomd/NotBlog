using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频到收藏夹命令处理器。
/// </summary>
public class AddVideoToCollectionCommandHandler(
    IVideoRepository videoRepository,
    IVideoCollectionRepository collectionRepository,
    IVideoCacheService cacheService,
    ILogger<AddVideoToCollectionCommandHandler> logger)
    : IRequestHandler<AddVideoToCollectionCommand, CollectionOperationResult>
{
    public async Task<CollectionOperationResult> Handler(AddVideoToCollectionCommand request,
        CancellationToken cancellationToken)
    {
        var collection = await collectionRepository.FindByVideoCollectionAsync(request.CollectionGuid);
        if (collection is null)
            return new CollectionOperationResult(false, "Collection not found.");

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
        var collection = await collectionRepository.FindByVideoCollectionAsync(request.CollectionGuid);
        if (collection is null)
            return new CollectionOperationResult(false, "Collection not found.");

        if (!collection.VideoGuid.Contains(request.VideoGuid))
            return new CollectionOperationResult(true, "Video not in collection."); // 幂等

        collection.VideoGuid.Remove(request.VideoGuid);
        await collectionRepository.UpdateByVideoCollectionAsync(collection);

        logger.LogInformation("Video {VideoGuid} removed from collection {CollectionGuid}",
            request.VideoGuid, request.CollectionGuid);

        return new CollectionOperationResult(true, null);
    }
}
