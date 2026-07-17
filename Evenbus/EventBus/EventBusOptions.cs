namespace Notcomd.Evenbus.EventBus;

/// <summary>
/// EventBus 配置选项（对齐 eShop EventBusOptions）
/// </summary>
public class EventBusOptions
{
    /// <summary>订阅客户端名称（队列名）</summary>
    public string SubscriptionClientName { get; set; } = "eventbus_queue";

    /// <summary>Polly 重试次数（默认 10 次，指数退避）</summary>
    public int RetryCount { get; set; } = 10;
}
