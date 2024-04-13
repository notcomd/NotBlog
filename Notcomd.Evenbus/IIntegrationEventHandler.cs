namespace Notcomd.Evenbus
{
    public interface IIntegrationEventHandler
    {
        Task Eventhander(string EventName, string EventData);
    }
}
