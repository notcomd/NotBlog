using System.Text.Json.Serialization;
using Notcomd.EventBus.Core;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限变更集成事件 — 与 Identity 侧 PermissionUpdatedIntegrationEvent 结构一致。
/// EventBus 按类型名匹配订阅，JSON 序列化跨服务传递，属性名需保持一致。
/// </summary>
public record PermissionUpdatedIntegrationEvent(
    [property: JsonPropertyName("PermissionId")] Guid PermissionId,
    [property: JsonPropertyName("Action")] string Action
) : IntegrationEvent
{
    [JsonPropertyName("UpdatedAt")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
