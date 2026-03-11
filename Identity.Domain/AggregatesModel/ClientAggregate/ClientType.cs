namespace Identity.Domain.AggregatesModel.ClientAggregate;

public enum ClientType
{
    /// <summary>
    ///     客户端
    /// </summary>
    Client = 0,

    /// <summary>
    ///     移动端
    /// </summary>
    Mobile = 1,

    /// <summary>
    ///     桌面端
    /// </summary>
    Desktop = 2,

    /// <summary>
    ///     Web端
    /// </summary>
    Web = 3
}