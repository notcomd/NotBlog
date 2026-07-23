using System.Diagnostics;
using OpenTelemetry.Context.Propagation;

namespace Notcomd.EventBus.EventBus;

/// <summary>
/// RabbitMQ OpenTelemetry 追踪集成
/// 对齐 eShop RabbitMQTelemetry
/// </summary>
public class RabbitMQTelemetry
{
    public static readonly string ActivitySourceName = "Notcomd.EventBus.RabbitMQ";

    public ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    public TextMapPropagator Propagator { get; } =
        Propagators.DefaultTextMapPropagator;
}
