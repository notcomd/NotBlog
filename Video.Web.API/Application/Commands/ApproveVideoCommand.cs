using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 审核通过视频命令（管理端，待审核 → 已通过并公开展示）。
/// </summary>
/// <param name="VideoGuid">视频 Guid</param>
public record ApproveVideoCommand(Guid VideoGuid) : IRequest<bool>;
