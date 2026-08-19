namespace Identity.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 权限变更集成事件 — 在权限 CRUD（创建/更新/删除）成功后发布到 RabbitMQ。
/// 网关（NotBlog_Yarp）订阅此事件后自动拉取最新路由映射，无需重启。
/// </summary>
/// <param name="PermissionId">变更的权限 ID</param>
/// <param name="Action">变更类型：create / update / delete</param>
public record PermissionUpdatedIntegrationEvent(
    Guid PermissionId,
    string Action) : IntegrationEvent
{
    /// <summary>变更时间</summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
