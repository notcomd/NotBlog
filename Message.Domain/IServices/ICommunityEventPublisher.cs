
namespace Message.Domain.IServices;

/// <summary>
/// 社区事件发布器（AI 机器人 / MCP 扩展点）。
/// <para>
/// 所有圈子/话题/关注领域事件在应用层统一转换为 <see cref="CommunityEventEnvelope"/> 经此接口发布。
/// 当前默认实现 <c>DefaultCommunityEventPublisher</c> 仅记录日志（No-op）；
/// 后续可替换为 RabbitMQ / Webhook / 流式实现，AI 机器人、MCP 网关订阅事件流即可，业务代码零侵入。
/// </para>
/// </summary>
public interface ICommunityEventPublisher
{
    /// <summary>发布一条社区事件</summary>
    Task PublishAsync(CommunityEventEnvelope envelope, CancellationToken cancellationToken = default);
}
