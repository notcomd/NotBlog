using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 添加视频到收藏夹命令 — 带幂等性保护，防止重复收藏。
/// </summary>
public record AddVideoToCollectionCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    Guid CollectionGuid) : IRequest<CollectionOperationResult>, IIdempotentRequest;

/// <summary>从收藏夹移除视频命令 — 带幂等性保护。</summary>
public record RemoveVideoFromCollectionCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    Guid CollectionGuid) : IRequest<CollectionOperationResult>, IIdempotentRequest;

/// <summary>收藏操作结果。</summary>
public record CollectionOperationResult(bool Success, string? ErrorMessage);
