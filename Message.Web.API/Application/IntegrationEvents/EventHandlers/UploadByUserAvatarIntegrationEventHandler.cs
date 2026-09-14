

namespace Message.Web.API.Application.IntegrationEvents.EventHandlers;


public class UploadByUserAvatarIntegrationEventHandler(
    IUserInfoRepository userInfoRepository,
    ILogger<UploadByUserAvatarIntegrationEventHandler> logger)
    : JsonIntegrationEventHandler<UploadByUserAvatarIntegrationEvent>
{
    public override async Task Handler(UploadByUserAvatarIntegrationEvent @event)
    {
        try
        {
            var userInfo = await userInfoRepository.GetByUserIdAsync(@event.UserId);
            if (userInfo is null)
            {
                // 资料缺失（注册事件丢失/消费顺序错乱）：凭事件携带的账号信息按注册语义补建，避免头像更新被丢弃
                if (string.IsNullOrWhiteSpace(@event.Email))
                {
                    logger.LogWarning(
                        "用户资料不存在且事件未携带邮箱，无法补建资料，跳过本次头像更新：UserId={UserId}",
                        @event.UserId);
                    return;
                }

                userInfo = UserInfo.Create(@event.UserId, @event.Email, @event.NickName, @event.AvatarUrl);
                await userInfoRepository.AddAsync(userInfo);
                await userInfoRepository.UnitOfWork.SaveEntitiesAsync();

                logger.LogInformation("用户资料缺失，已按头像事件补建资料：UserId={UserId}", @event.UserId);
                return;
            }

            userInfo.UpdateAvatar(@event.AvatarUrl);
            await userInfoRepository.UpdateAsync(userInfo);
            await userInfoRepository.UnitOfWork.SaveEntitiesAsync();

            logger.LogInformation("用户 {UserId} 头像已更新", @event.UserId);
        }
        catch (Exception ex)
        {
            // P1-B：记录带用户上下文的日志后继续抛出，交由事件总线 nack(requeue:false) 投递死信队列，
            // 而不是吞掉异常导致消息被 ACK、失败无迹可查。
            logger.LogError(ex, "消费上传头像事件更新用户资料失败：UserId={UserId}", @event.UserId);
            throw;
        }
    }
}

[EventBusName("UploadByUserAvatarIntegrationEvent")]
public record UploadByUserAvatarIntegrationEvent(
    Guid UserId,
    Uri AvatarUrl,
    string? Email = null,
    string? NickName = null) : IntegrationEvent
{
    public DateTime UploadTime { get; init; } = DateTime.UtcNow;
}
