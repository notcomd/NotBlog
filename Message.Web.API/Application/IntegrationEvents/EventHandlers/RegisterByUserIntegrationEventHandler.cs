namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;

/// <summary>
/// 用户注册集成事件处理器（消费 Identity 发布的 RegisterByUserIntegrationEvent）。
/// <para>职责：为新注册用户创建 UserInfo 资料（等级 1/经验 0/硬币 0）；幂等——已存在则跳过；失败仅记日志。</para>
/// </summary>
[EventBusName("RegisterByUserIntegrationEvent")]
public class RegisterByUserIntegrationEventHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<RegisterByUserIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<RegisterByUserMessageIntegrationEvent>
{
    public override async Task Handler(RegisterByUserMessageIntegrationEvent @event)
    {
        try
        {
            var existing = await userInfoRepository.GetByUserIdAsync(@event.UserId);
            if (existing is not null)
            {
                logger.LogDebug("用户资料已存在，跳过创建：UserId={UserId}", @event.UserId);
                return;
            }

            var userInfo = UserInfo.Create(@event.UserId, @event.Email, @event.NickName, @event.AvatarUrl);
            await userInfoRepository.AddAsync(userInfo);
            await userInfoRepository.UnitOfWork.SaveEntitiesAsync();

            logger.LogInformation("注册事件已消费，创建用户资料：UserId={UserId}", @event.UserId);
        }
        catch (Exception ex)
        {
            // P1-B：记录日志后继续抛出，交由事件总线 nack(requeue:false) 投递死信队列；
            // 消息重放时上方"已存在则跳过"保证幂等。
            logger.LogError(ex, "消费注册事件创建用户资料失败：UserId={UserId}", @event.UserId);
            throw;
        }
    }
}

/// <summary>
/// 用户注册集成事件数据副本（字段与 Identity 发布侧一致；跨服务不共享程序集，各自维护）。
/// <para>routing key = 类型名 RegisterByUserIntegrationEvent（发布侧无 [EventBusName]，与 handler 类特性对齐）。</para>
/// </summary>
[EventBusName("RegisterByUserMessageIntegrationEvent")]
public record RegisterByUserMessageIntegrationEvent(Guid UserId, string Email, string? NickName = null, Uri? AvatarUrl = null) : IntegrationEvent
{
    /// <summary>注册时间（默认当前时间）</summary>
    public DateTime RegisterTime { get; init; } = DateTime.UtcNow;
}
