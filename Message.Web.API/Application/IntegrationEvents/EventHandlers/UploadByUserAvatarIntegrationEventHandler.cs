

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
                logger.LogWarning("用户资料不存在，无法更新头像：UserId={UserId}", @event.UserId);
                return;
            }

            userInfo.UpdateAvatar(@event.AvatarUrl);
            await userInfoRepository.UpdateAsync(userInfo);
            await userInfoRepository.UnitOfWork.SaveEntitiesAsync();

            logger.LogInformation("用户 {UserId} 头像已更新", @event.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "消费上传头像事件更新用户资料失败：UserId={UserId}", @event.UserId);
        }
    }
}

[EventBusName("UploadByUserAvatarIntegrationEvent")]
public record UploadByUserAvatarIntegrationEvent(Guid UserId, Uri AvatarUrl) : IntegrationEvent
{
    public DateTime UploadTime { get; init; } = DateTime.UtcNow;
}