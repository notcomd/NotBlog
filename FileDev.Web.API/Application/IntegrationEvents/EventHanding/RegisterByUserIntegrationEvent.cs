using Notcomd.EventBus.Core;

namespace FileDev.Web.API.Application.IntegrationEvents.EventHanding;

/// <summary>
/// 用户注册集成事件 — 与 Identity 服务发布的 JSON 结构一致。
/// 放在消费端项目中避免对 Identity 项目的编译依赖。
/// </summary>
public record RegisterByUserIntegrationEvent(Guid UserId) : IntegrationEvent
{
    public DateTime RegisterTime { get; init; }
}
