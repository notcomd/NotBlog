using Evenbus.Core;
using Notcomd.Evenbus;

namespace Evenbus.IntegrationTest;

/// <summary>订单确认集成事件（用于 RabbitMQ 真实连接测试）</summary>
[EvenBusName("Test.Order.Confirmed")]
public record OrderConfirmedIntegrationEvent(
    Guid OrderId,
    string CustomerName,
    decimal Amount,
    string ProductName,
    DateTime ConfirmedAt
) : IntegrationEvent;
