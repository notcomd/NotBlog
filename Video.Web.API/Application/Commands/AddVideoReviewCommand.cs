using NotMediator;
using Video.Domain.ValueObjects;
using Video.Web.API.Application.Behaviors;

namespace Video.Web.API.Application.Commands;

/// <summary>添加视频评论命令 — 带幂等性保护。</summary>
public record AddVideoReviewCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    Guid? RootReview,
    string? Body,
    List<VideoImage>? VideoImages,
    string? ContentType = null) : IRequest<bool>, IIdempotentRequest;
