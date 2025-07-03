namespace Identity.Domain.Entities;

public enum StatusCode
{

    /// <summary>
    /// 正常
    /// </summary>
    Ok = 200,

    /// <summary>
    /// 错误
    /// </summary>
    Error = 500,

    /// <summary>
    /// 超时
    /// </summary>
    TimeOut = 502,

    /// <summary>
    /// 重置
    /// </summary>
    Reset = 503,

    /// <summary>
    /// 未授权
    /// </summary>
    NotAuthorized = 401,
}