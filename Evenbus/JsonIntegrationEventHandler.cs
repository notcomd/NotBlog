using System.Text.Json;

namespace Notcomd.Evenbus;

public abstract class JsonIntegrationEventHandler<T> : IIntegrationEventHandler
{
    public Task Handler(string eventName, string eventData)
    {
        var data = JsonSerializer.Deserialize<T>(eventData);
        return EventDlerJson(eventName, data);
    }

    protected abstract Task EventDlerJson(string eventName, T? eventData);
}