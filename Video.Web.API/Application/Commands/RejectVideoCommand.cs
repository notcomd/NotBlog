using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 驳回视频命令（管理端，待审核 → 已拒绝并记录原因）。
/// </summary>
/// <param name="VideoGuid">视频 Guid</param>
/// <param name="Reason">驳回原因（不可为空，长度上限 500）</param>
public record RejectVideoCommand(Guid VideoGuid, string Reason) : IRequest<bool>;
