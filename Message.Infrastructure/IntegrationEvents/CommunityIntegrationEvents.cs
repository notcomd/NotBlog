
namespace Message.Infrastructure.IntegrationEvents;

/// <summary>
/// 社区事件集成事件（跨服务 / AI 机器人 / MCP 网关订阅入口）。
/// <para>
/// 统一信封设计：所有社区领域事件（圈子/话题/关注/帖子）以 <c>CommunityEventEnvelope</c> 载荷
/// 经此事件发布到 EventBus（Direct exchange，路由键 <c>CommunityEvent.Published</c>），
/// 订阅方按 <see cref="EventType"/> 分派处理，无需为每种事件单独建队列。
/// </para>
/// <para>消息体字段与 Message.Domain.Events.CommunityEventEnvelope 一一对应。</para>
/// </summary>
[EventBusName("CommunityEvent.Published")]
public record CommunityEventEnvelopeIntegrationEvent(
    /// <summary>事件类型（如 circle.post.published / user.followed）</summary>
    string EventType,
    /// <summary>所属圈子（全局事件为空）</summary>
    Guid? CircleGuid,
    /// <summary>触发者</summary>
    Guid ActorGuid,
    /// <summary>目标（帖子/评论/用户）</summary>
    Guid? TargetGuid,
    /// <summary>附加载荷（序列化 JSON）</summary>
    string? PayloadJson,
    /// <summary>发生时间</summary>
    DateTimeOffset OccurredAt) : IntegrationEvent;
