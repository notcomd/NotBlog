using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 提交视频审核命令（作者本人，草稿/被驳回 → 待审核）。
/// </summary>
/// <param name="VideoGuid">视频 Guid</param>
/// <param name="UserGuid">调用者 Guid（服务端解析，须为作者本人）</param>
public record SubmitVideoForReviewCommand(Guid VideoGuid, Guid UserGuid) : IRequest<bool>;
