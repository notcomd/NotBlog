namespace Notcomd.Evenbus.EventBus;

public class IntegrationEventRabbitMqOptions
{
    public string HostName { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string? UserName { get; set; }
    public string? Password { get; set; }

    /// <summary>Exchange 类型（默认 Direct）</summary>
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Direct;

    /// <summary>死信队列名称后缀</summary>
    public string DeadLetterQueueSuffix { get; set; } = ".dlq";

    /// <summary>消费者最大重试次数</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>重试间隔（毫秒）</summary>
    public int RetryIntervalMs { get; set; } = 1000;

    /// <summary>RPC 请求超时（秒）</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}