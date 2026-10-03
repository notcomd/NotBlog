
namespace Video.Domain.Events;

/// <summary>
/// 视频上传完成领域事件（由 Videos 聚合在创建后触发，交由领域事件处理器分发）。
/// </summary>
/// <param name="VideoId">视频 Guid</param>
/// <param name="VideoName">视频名称</param>
/// <param name="VideoUrl">视频文件地址</param>
public record UploadVideoDomainEvent(Guid VideoId, string VideoName, string VideoUrl):INotifications;
