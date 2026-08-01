using NotMediator;
using Video.Web.API.Application.Behaviors;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 视频点赞/取消点赞命令 — 通过分布式锁（Redis）确保幂等性。
/// Field 支持: upvote（点赞）、down（踩）、ballot（投票）、share（分享）
/// </summary>
public record LikeVideoCommand(
    Guid RequestId,
    Guid VideoGuid,
    Guid UserGuid,
    string Field,
    bool IsLike) : IRequest<LikeVideoResult>, IIdempotentRequest;

/// <summary>视频点赞操作结果。</summary>
public record LikeVideoResult(bool Success, long NewCount, string? ErrorMessage);
