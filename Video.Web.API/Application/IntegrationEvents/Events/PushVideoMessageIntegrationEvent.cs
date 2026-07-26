using Notcomd.EventBus.Core;

namespace Video.Web.API.Application.IntegrationEvents.Events;

public record PushVideoMessageIntegrationEvent(Guid VideoGuid, HashSet<Guid> AffectedUserGuid, 
string VideoName, string VideoFileUri):IntegrationEvent
{
    public DateTime OccurredOn => DateTime.UtcNow;
}