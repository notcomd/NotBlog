
namespace Message.Infrastructure.Services;

/// <summary>
/// 社区事件发布器默认实现（No-op）。
/// <para>
/// 仅记录日志后丢弃，保证当前零外部依赖运行；
/// 后续接入 AI 机器人 / MCP 时替换为 RabbitMQ / Webhook 实现（实现同一接口即可，业务代码零侵入）。
/// </para>
/// </summary>
public class DefaultCommunityEventPublisher(
    ILogger<DefaultCommunityEventPublisher> logger) : ICommunityEventPublisher
{
    /// <summary>发布社区事件；默认实现仅记录日志后丢弃，不产生外部副作用。</summary>
    public Task PublishAsync(CommunityEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[CommunityEvent] Type={EventType}, Circle={CircleGuid}, Actor={ActorGuid}, Target={TargetGuid}, At={OccurredAt}",
            envelope.EventType, envelope.CircleGuid, envelope.ActorGuid, envelope.TargetGuid, envelope.OccurredAt);

        // TODO(AI/MCP): 替换为 RabbitMqCommunityEventPublisher / WebhookCommunityEventPublisher，
        // 将 envelope 序列化后发布到事件总线，AI 机器人、MCP 网关订阅消费。
        return Task.CompletedTask;
    }
}
