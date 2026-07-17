using Notcomd.Evenbus;

namespace Evenbus.Core;



public interface IIntegrationEventHandler<in TIntegrationEvent>:IIntegrationEventHandler where TIntegrationEvent : IntegrationEvent
{
    Task Handler(TIntegrationEvent @event);

    Task IIntegrationEventHandler.Handler(IntegrationEvent @event)=>Handler((TIntegrationEvent)@event);
  
}

public interface IIntegrationEventHandler
{
    Task Handler(IntegrationEvent @event);
}

