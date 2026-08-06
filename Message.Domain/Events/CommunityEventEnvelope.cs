namespace Message.Domain.Events;

/// <summary>
/// 社区事件信封：ICommunityEventPublisher 的统一出口载荷。
/// <para>后续 AI 机器人 / MCP 网关可订阅该事件流（RabbitMQ / Webhook），业务代码零侵入。</para>
/// </summary>
/// <param name="EventType">事件类型（如 circle.post.published / user.followed）</param>
/// <param name="CircleGuid">所属圈子（全局事件为空）</param>
/// <param name="ActorGuid">触发者</param>
/// <param name="TargetGuid">目标（帖子/评论/用户）</param>
/// <param name="PayloadJson">附加载荷（序列化 JSON）</param>
/// <param name="OccurredAt">发生时间</param>
public sealed record CommunityEventEnvelope(
    string EventType,
    Guid? CircleGuid,
    Guid ActorGuid,
    Guid? TargetGuid,
    string? PayloadJson,
    DateTimeOffset OccurredAt)
{
    public static CommunityEventEnvelope Create(
        string eventType, Guid actorGuid, Guid? circleGuid = null, Guid? targetGuid = null,
        object? payload = null) => new(
        eventType, circleGuid, actorGuid, targetGuid,
        payload is null ? null : System.Text.Json.JsonSerializer.Serialize(payload),
        DateTimeOffset.UtcNow);
}
