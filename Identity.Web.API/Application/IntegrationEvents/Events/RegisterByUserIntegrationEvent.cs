namespace Identity.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 用户注册成功集成事件（发布到 RabbitMQ，下游服务消费）
/// </summary>
public record RegisterByUserIntegrationEvent(Guid UserId) : IntegrationEvent
{
    public DateTime RegisterTime { get; init; } = DateTime.UtcNow;
}
