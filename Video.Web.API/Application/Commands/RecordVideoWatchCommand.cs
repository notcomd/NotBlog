using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 记录视频观看命令 — 带幂等性保护，防止重复统计。
/// </summary>
public record RecordVideoWatchCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    double Progress,
    double LastPositionSeconds) : IRequest<WatchRecordResult>, IIdempotentRequest;

/// <summary>结束视频观看命令 — 带幂等性保护。</summary>
public record EndVideoWatchCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid) : IRequest<WatchRecordResult>, IIdempotentRequest;

/// <summary>观看记录结果。</summary>
public record WatchRecordResult(bool Success, Guid? WatchHistoryGuid, string? ErrorMessage);
