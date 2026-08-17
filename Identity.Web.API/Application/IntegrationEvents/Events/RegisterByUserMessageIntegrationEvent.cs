namespace Identity.Web.API.Application.IntegrationEvents.Events;

public record RegisterByUserMessageIntegrationEvent(Guid UserId, string UserName, Uri AvatarUrl) : IntegrationEvent
{
    public DateTime RegisterTime { get; init; } = DateTime.UtcNow;
}

