namespace Identity.Domain.Result;

/// <summary>
/// 领域结果对象 — 纯 POCO，不依赖任何 Web 框架
/// 
/// 在 Web.API 层通过扩展方法转换为 IResult 响应
/// </summary>
public sealed class IdentityResult<TResponse> where TResponse : class
{
    private IdentityResult(string resultMessage, StatusCode statusCode, TResponse resultData,
        ResultType resultType = ResultType.ApplicationJson)
    {
        ResultMessage = resultMessage;
        StatusCode = statusCode;
        ResultData = resultData;
        ResultType = resultType;
    }

    /// <summary>
    ///     返回结果消息
    /// </summary>
    public string ResultMessage { get; set; }

    /// <summary>
    ///     状态码
    /// </summary>
    public StatusCode StatusCode { get; set; }

    /// <summary>
    ///     返回数据
    /// </summary>
    public TResponse? ResultData { get; set; }

    /// <summary>
    ///     返回格式类型
    /// </summary>
    public ResultType ResultType { get; set; }

    // ── 工厂方法 ──

    public static IdentityResult<TResponse> Success(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.Ok, data, resultType);
    }

    public static IdentityResult<TResponse> Error(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.Error, data, resultType);
    }

    public static IdentityResult<TResponse> TimeOut(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.TimeOut, data, resultType);
    }

    public static IdentityResult<TResponse> Reset(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.Reset, data, resultType);
    }

    public static IdentityResult<TResponse> NotAuthorized(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.NotAuthorized, data, resultType);
    }

    public static IdentityResult<TResponse> InternalServerError(string message, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, StatusCode.InternalServerError, data, resultType);
    }

    public static IdentityResult<TResponse> Other(string message, StatusCode statusCode, TResponse data,
        ResultType resultType = ResultType.ApplicationJson)
    {
        return new IdentityResult<TResponse>(message, statusCode, data, resultType);
    }
}
