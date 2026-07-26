using NotMediator;
using Video.Domain.Cache;
using Video.Domain.IRepository;
using Video.Domain.Entities;
namespace Video.Web.API.Application.Commands;

public class AddVideoBarrageCommandHandler : IRequestHandler<AddVideoBarrageCommand, Guid>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoCacheService _cacheService;

    public AddVideoBarrageCommandHandler(IVideoRepository videoRepository, IVideoCacheService cacheService)
    {
        _videoRepository = videoRepository;
        _cacheService = cacheService;
    }

    /// <summary>
    /// 添加视频弹幕（支持文本/图片/混合）。
    /// </summary>
    public async Task<Guid> Handler(AddVideoBarrageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasText = !string.IsNullOrWhiteSpace(request.Body);
        var hasImages = request.VideoImages is { Count: > 0 };

        if (!hasText && !hasImages)
            throw new ArgumentException("弹幕必须包含文本或图片内容");

        var video = await _videoRepository.FindByVideoAsync(request.VideoGuid);
        ArgumentNullException.ThrowIfNull(video);

        VideoBarrage barrage = (hasText, hasImages) switch
        {
            (true, true) => VideoBarrage.CreateMixed(request.UserGuid, request.Body!, request.VideoImages!),
            (true, false) => VideoBarrage.CreateText(request.UserGuid, request.Body!),
            (false, true) => VideoBarrage.CreateImage(request.UserGuid, request.VideoImages!),
            _ => throw new InvalidOperationException("Invalid barrage content state.")
        };

        video.AddByVideoBarrage(barrage);
        await _videoRepository.UpdateByVideoAsync(video);

        if (_cacheService is not null)
            await _cacheService.RemoveVideoMetaAsync(request.VideoGuid, cancellationToken);

        return barrage.VideoBarrageGuid;
    }
}
