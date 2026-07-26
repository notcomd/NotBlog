using NotMediator;
using Video.Domain.Server;
using Video.Domain.ValueObjects;

namespace Video.Web.API.Application.Commands;

/// <summary>Add a review to a video.</summary>
public record AddVideoReviewCommand(
    Guid VideoGuid,
    Guid UserGuid,
    Guid? RootReview,
    string? Body,
    List<VideoImage>? VideoImages,
    string? ContentType = null) : IRequest<bool>;

