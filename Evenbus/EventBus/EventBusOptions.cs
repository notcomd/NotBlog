namespace Notcomd.Evenbus.EventBus;

/// <summary>
/// EventBus 统一配置选项
/// 
/// 支持通过 appsettings.json 的 "EventBus" 节进行绑定：
/// {
///   "EventBus": {
///     "SubscriptionClientName": "identity_events",
///     "RetryCount": 5,
///     "ExchangeName": "notcomd_event_bus",
///     "ExchangeType": "Direct",
///     "DeadLetterQueueSuffix": ".dlq",
///     "PrefetchCount": 10,
///     "MaxConcurrency": 4
///   }
/// }
/// 
/// Aspire 集成：bunlder.AddRabbitMQClient("EventBus") 自动注册 IConnectionFactory。
/// </summary>
public class EventBusOptions
{
    /// <summary>配置节名称</summary>
    public const string SectionName = "EventBus";

    // ═══ 队列与订阅 ═══

    /// <summary>订阅客户端名称（队列名）</summary>
    public string SubscriptionClientName { get; set; } = "eventbus_queue";

    // ═══ 连接重试 ═══

    /// <summary>Polly 重试次数（默认 10 次，指数退避）</summary>
    public int RetryCount { get; set; } = 10;

    /// <summary>重试退避基数（秒），第 N 次重试延迟 = base^N 秒</summary>
    public double RetryBackoffBase { get; set; } = 2.0;

    // ═══ Exchange ═══

    /// <summary>Exchange 名称</summary>
    public string ExchangeName { get; set; } = "notcomd_event_bus";

    /// <summary>Exchange 类型（Direct / Fanout / Topic）</summary>
    public ExchangeType ExchangeType { get; set; } = ExchangeType.Direct;

    // ═══ 死信队列（DLQ）═══

    /// <summary>死信队列名称后缀</summary>
    public string DeadLetterQueueSuffix { get; set; } = ".dlq";

    // ═══ 消费性能 ═══

    /// <summary>消费者预取数量（0 = 无限制）</summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>最大并发消息处理数（1 = 串行）</summary>
    public int MaxConcurrency { get; set; } = 1;

    // ═══ RPC ═══

    /// <summary>RPC 请求超时（秒）</summary>
    public int RequestTimeoutSeconds { get; set; } = 30;
}
