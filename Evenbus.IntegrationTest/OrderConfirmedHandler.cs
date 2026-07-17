using Evenbus.Core;
using Microsoft.Extensions.Logging;

namespace Evenbus.IntegrationTest;

/// <summary>订单确认事件处理器（打印到控制台）</summary>
public class OrderConfirmedHandler(ILogger<OrderConfirmedHandler> logger)
    : JsonIntegrationEventHandler<OrderConfirmedIntegrationEvent>
{
    public override Task Handler(OrderConfirmedIntegrationEvent @event)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"""
            ╔══════════════════════════════════════╗
            ║  收到集成事件！                       ║
            ╠══════════════════════════════════════╣
            ║  事件ID:   {@event.Id}
            ║  订单ID:  {@event.OrderId}
            ║  客户:    {@event.CustomerName}
            ║  金额:    {@event.Amount:C}
            ║  商品:    {@event.ProductName}
            ║  时间:    {@event.ConfirmedAt:yyyy-MM-dd HH:mm:ss}
            ╚══════════════════════════════════════╝
            """);
        Console.ResetColor();

        logger.LogInformation(
            "[Handler] 订单集成事件已处理: OrderId={OrderId}, Amount={Amount}",
            @event.OrderId, @event.Amount);

        return Task.CompletedTask;
    }
}
