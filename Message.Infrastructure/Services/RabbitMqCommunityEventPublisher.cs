
namespace Message.Infrastructure.Services;

/// <summary>
/// 社区事件发布器（RabbitMQ 实现）：ICommunityEventPublisher 的事件总线出口。
/// <para>
/// 链路：领域事件 → CommunityEventEnvelope → <see cref="CommunityEventEnvelopeIntegrationEvent"/>
/// → IEventBus.PublishAsync（Direct exchange，路由键 CommunityEvent.Published）。
/// AI 机器人 / MCP 网关等外部订阅方绑定同一 exchange 消费即可，Message 服务无需感知订阅方。
/// </para>
/// <para>容错语义：发布失败仅记录错误日志，不影响业务主流程（与 No-op 实现行为一致）；</para>
/// <para>如需不丢事件，可启用 Eventbus.Outbox（EfCoreOutboxStore + OutboxPublisher）。</para>
/// </summary>
public class RabbitMqCommunityEventPublisher(
    IEventBus eventBus,
    ILogger<RabbitMqCommunityEventPublisher> logger) : ICommunityEventPublisher
{
    public async Task PublishAsync(CommunityEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            var integrationEvent = new CommunityEventEnvelopeIntegrationEvent(
                envelope.EventType,
                envelope.CircleGuid,
                envelope.ActorGuid,
                envelope.TargetGuid,
                envelope.PayloadJson,
                envelope.OccurredAt);

            await eventBus.PublishAsync(integrationEvent);

            logger.LogDebug("社区事件已发布到事件总线: Type={EventType}, Circle={CircleGuid}, Actor={ActorGuid}",
                envelope.EventType, envelope.CircleGuid, envelope.ActorGuid);
        }
        catch (Exception ex)
        {
            // 容错：事件总线故障不影响业务主流程；启用 Outbox 后可保证不丢
            logger.LogError(ex, "社区事件发布到 RabbitMQ 失败: Type={EventType}", envelope.EventType);
        }
    }
}
