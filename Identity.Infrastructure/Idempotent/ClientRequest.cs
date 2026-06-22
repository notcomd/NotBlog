namespace Identity.Infrastructure.Idempotent;

/// <summary>
/// 客户端请求实体
/// </summary>
public class ClientRequest
{
    /// <summary>
    /// 客户端请求 ID
    /// </summary>
    public Guid ClientRequestId { get; set; }

    /// <summary>
    /// 客户端请求名称
    /// </summary>
    public string ClientRequestName { get; set; } = null!;

    /// <summary>
    /// 创建日期
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }
}