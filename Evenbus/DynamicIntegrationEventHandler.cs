using Dynamic.Json;

namespace Notcomd.Evenbus;

public abstract class DynamicIntegrationEventHandler : IIntegrationEventHandler
{
    public Task Handler(string eventName, string eventData)
    {
        //https://github.com/dotnet/runtime/issues/53195
        //https://github.com/dotnet/core/issues/6444
        //.NET 6目前不支持把json反序列化为dynamic，本来preview 4支持，但是在preview 7又去掉了
        //所以暂时用Dynamic.Json来实现。
        var dynamicEventData = DJson.Parse(eventData);
        return HandleDynamic(eventName, dynamicEventData);
    }

    protected abstract Task HandleDynamic(string eventName, dynamic eventData);
}