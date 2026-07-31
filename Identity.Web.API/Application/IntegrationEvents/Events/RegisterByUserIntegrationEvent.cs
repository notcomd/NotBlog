namespace Identity.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 用户注册成功集成事件（发布到 RabbitMQ，下游服务消费）
/// </summary>
/// <remarks name="UserId">用户ID</remarks>
public record RegisterByUserIntegrationEvent(Guid UserId) : IntegrationEvent
{
    /// <summary>
    /// 注册时间
    /// </summary>
    /// <remarks>默认值为当前时间</remarks>
    public DateTime RegisterTime { get; init; } = DateTime.UtcNow;
}
