
namespace Identity.Web.API.Application.IntegrationEvents.Events;

public record UploadByUserAvatarIntegrationEvent(Guid UserId, Uri AvatarUrl) : IntegrationEvent
{
    public DateTime UploadTime { get; init; } = DateTime.UtcNow;
}