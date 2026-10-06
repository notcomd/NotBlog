namespace Video.Web.API.Dto.Request;

/// <summary>驳回视频请求体（管理端审核）。</summary>
/// <param name="Reason">驳回原因（不可为空，长度上限 500）</param>
public record RequestRejectVideo(string Reason);
