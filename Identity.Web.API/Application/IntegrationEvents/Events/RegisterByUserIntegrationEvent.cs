namespace Identity.Web.API.Application.IntegrationEvents.Events;

/// <summary>
/// 用户注册成功集成事件（发布到 RabbitMQ，下游服务消费）。
/// 携带注册邮箱/昵称/头像，供 Message 等下游初始化 UserInfo 读侧投影（Identity 为唯一真相源）。
/// </summary>
public record RegisterByUserIntegrationEvent(
    Guid UserId,
    string Email,
    string? NickName = null,
    Uri? AvatarUrl = null) : IntegrationEvent
{
    /// <summary>
    /// 注册时间
    /// </summary>
    /// <remarks>默认值为当前时间</remarks>
    public DateTime RegisterTime { get; init; } = DateTime.UtcNow;
}
