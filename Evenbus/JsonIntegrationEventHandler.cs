using System.Text.Json;

namespace Notcomd.Evenbus;

public abstract class JsonIntegrationEventHandler<T> : IIntegrationEventHandler
{
    public Task Eventhander(string EventName, string EventData)
    {
        var eventData = JsonSerializer.Deserialize<T>(EventData);
        return EventDlerJson(EventName, eventData);
    }

    public abstract Task EventDlerJson(string eventName, T? eventData);
}