namespace CommonsInitializer;

/// <summary>
/// 初始化器配置选项
/// </summary>
public class InitializerOptions
{
    /// <summary>日志文件路径</summary>
    public string? LogFilePath { get; set; }

    /// <summary>EventBus 队列名，同一项目值需一致，不同项目不能冲突</summary>
    public string? EventBusQueueName { get; set; }
}