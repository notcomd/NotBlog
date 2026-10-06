namespace Video.Web.API.Dto.Response;

/// <summary>视频状态列表项 DTO（「我的视频」与「管理端审核列表」复用）。</summary>
/// <param name="VideoGuid">视频 Guid</param>
/// <param name="VideoName">视频名称</param>
/// <param name="BriefIntroduction">视频简介</param>
/// <param name="VideoCover">封面地址</param>
/// <param name="Status">内容审核状态（Draft/Pending/Approved/Rejected）</param>
/// <param name="RejectReason">审核驳回原因（仅 Rejected 时非空）</param>
/// <param name="CreateTime">创建时间</param>
/// <param name="Visibility">可见性（AuthorVideo：VideoPublic/VideoPrivate/VideoProtected）</param>
public record MyVideoDto(
    Guid VideoGuid,
    string VideoName,
    string BriefIntroduction,
    string? VideoCover,
    string Status,
    string? RejectReason,
    DateTimeOffset CreateTime,
    string Visibility);
