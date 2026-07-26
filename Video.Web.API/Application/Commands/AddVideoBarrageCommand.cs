using NotMediator;
using Video.Domain.ValueObjects;

namespace Video.Web.API.Application.Commands;

/// <summary>Add a barrage (danmaku) to a video. Supports text, image, and mixed content.</summary>
public record AddVideoBarrageCommand(
    Guid VideoGuid,
    Guid UserGuid,
    string? Body,
    List<VideoImage>? VideoImages) : IRequest<Guid>;
