using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;
using Video.Domain.Entities;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频弹幕处理器（支持文本/图片/混合）。
/// </summary>
public class AddVideoBarrageCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService)
    : IRequestHandler<AddVideoBarrageCommand, Guid>
{
    public async Task<Guid> Handler(AddVideoBarrageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasText = !string.IsNullOrWhiteSpace(request.Body);
        var hasImages = request.VideoImages is { Count: > 0 };

        if (!hasText && !hasImages)
            throw new ArgumentException("弹幕必须包含文本或图片内容");

        var video = await videoRepository.FindByVideoAsync(request.VideoGuid);
        ArgumentNullException.ThrowIfNull(video);

        VideoBarrage barrage = (hasText, hasImages) switch
        {
            (true, true) => VideoBarrage.CreateMixed(request.UserGuid, request.Body!, request.VideoImages!),
            (true, false) => VideoBarrage.CreateText(request.UserGuid, request.Body!),
            (false, true) => VideoBarrage.CreateImage(request.UserGuid, request.VideoImages!),
            _ => throw new InvalidOperationException("Invalid barrage content state.")
        };

        video.AddByVideoBarrage(barrage);
        await videoRepository.UpdateByVideoAsync(video);

        if (cacheService is not null)
            await cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        return barrage.VideoBarrageGuid;
    }
}
