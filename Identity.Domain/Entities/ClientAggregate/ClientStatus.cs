namespace Identity.Domain.Entities.ClientAggregate;

/// <summary>
/// OAuth 2.0 客户端注册状态
/// </summary>
public enum ClientStatus
{
    /// <summary>活跃</summary>
    Active = 0,

    /// <summary>已禁用</summary>
    Disabled = 1,

    /// <summary>已吊销（密钥泄露或主动撤销）</summary>
    Revoked = 2
}
