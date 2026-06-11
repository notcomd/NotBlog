namespace Notcomd.Evenbus;

/// <summary>
/// Exchange 类型枚举
/// </summary>
public enum ExchangeType
{
    /// <summary>精确匹配 routing key（默认）</summary>
    Direct,

    /// <summary>广播到所有队列</summary>
    Fanout,

    /// <summary>模式匹配 routing key</summary>
    Topic
}