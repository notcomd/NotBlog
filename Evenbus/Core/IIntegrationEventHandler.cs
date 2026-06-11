namespace Notcomd.Evenbus;

public interface IIntegrationEventHandler
{
    Task Handler(string eventName, string message);
}