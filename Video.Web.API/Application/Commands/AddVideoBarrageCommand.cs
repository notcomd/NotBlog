using NotMediator;
using Video.Domain.ValueObjects;
using Video.Web.API.Application.Behaviors;

namespace Video.Web.API.Application.Commands;

/// <summary>添加弹幕命令 — 支持文本/图片/混合，带幂等性保护。</summary>
public record AddVideoBarrageCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    string? Body,
    List<VideoImage>? VideoImages) : IRequest<Guid>, IIdempotentRequest;
