using Notcomd.EventBus.Core;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 文件删除集成事件载荷。
/// </summary>
public record FileDeletedEventData(Guid FileId, Guid UserId, string FileName) : IntegrationEvent;
