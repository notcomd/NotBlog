namespace Notcomd.Evenbus.EventBus;

/// <summary>
/// RabbitMQ 集成事件配置选项（保留 NotBlog 特有配置）
/// </summary>
public class IntegrationEventRabbitMqOptions
{
    /// <summary>Exchange 名称</summary>
    public string ExchangeName { get; set; } = "notcomd_event_bus";

    /// <summary>Exchange 类型（默认 Direct）</summary>
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Direct;

    /// <summary>死信队列名称后缀</summary>
    public string DeadLetterQueueSuffix { get; set; } = ".dlq";

    /// <summary>RPC 请求超时（秒）</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}
