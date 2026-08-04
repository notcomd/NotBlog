using Notcomd.EventBus.Core;
using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 文件创建集成事件载荷。生产端由文件元数据持久化触发，消费端仅做日志记录。
/// </summary>
public record FileCreatedEventData(Guid FileId, Guid UserId, string FileName, long FileSize, string FileType) : IntegrationEvent;
