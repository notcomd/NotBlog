namespace Commons.Result;

/// <summary>
/// 非泛型统一 API 响应信封（无业务数据场景，如纯消息型操作结果）。
/// <para>
/// 与 <see cref="ApiResponseResult{TResponse}"/> 共同构成全站统一的 HTTP JSON 响应形状：
/// <c>{ statusCode, message, responseData, isSuccess, responseDateTime }</c>（默认 camelCase 序列化）。
/// 端点显式返回本类型实例时，<see cref="Web.ApiResponseWrappingMiddleware"/> 通过透传约定直接放行，避免二次包装。
/// </para>
/// </summary>
public sealed class ApiResponseResult
{
    /// <summary>HTTP 状态码（与真实响应状态码保持一致）。</summary>
    public long StatusCode { get; }

    /// <summary>面向客户端的提示消息（成功为 null 时序列化输出 null）。</summary>
    public string Message { get; }

    /// <summary>业务数据载荷；无数据场景为 null。</summary>
    public object? ResponseData { get; }

    /// <summary>是否成功（HTTP 2xx 为 true，其余为 false）。</summary>
    public bool IsSuccess { get; }

    /// <summary>响应生成时间（UTC）。</summary>
    public DateTime ResponseDateTime { get; }

    /// <summary>私有构造：统一经工厂创建，保证 <see cref="IsSuccess"/> 与 <see cref="StatusCode"/> 语义一致。</summary>
    private ApiResponseResult(long statusCode, string message, object? responseData, bool isSuccess)
    {
        StatusCode = statusCode;
        Message = message;
        ResponseData = responseData;
        IsSuccess = isSuccess;
        ResponseDateTime = DateTime.UtcNow;
    }

    /// <summary>成功信封（HTTP 200，无业务数据）。</summary>
    /// <param name="message">提示消息；为 null 时使用默认文案「请求成功」。</param>
    public static ApiResponseResult Ok(string? message = null)
        => new(200, message ?? "请求成功", null, true);

    /// <summary>失败信封（默认 HTTP 400），供错误处理中间件 / 无数据错误路径使用。</summary>
    /// <param name="message">领域错误消息。</param>
    /// <param name="statusCode">HTTP 状态码，默认 400。</param>
    /// <param name="responseData">可选的附加错误信息载荷。</param>
    public static ApiResponseResult Failure(string message, long statusCode = 400, object? responseData = null)
        => new(statusCode, message, responseData, false);

    /// <summary>成功信封（HTTP 204，无业务数据）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">提示消息；为 null 时使用默认文案「请求成功」。</param>
    public static ApiResponseResult NoContent(string? message = null)
        => new(204, message ?? "请求成功", null, true);

    /// <summary>失败信封（HTTP 400）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">错误消息。</param>
    /// <param name="code">HTTP 状态码，默认 400。</param>
    public static ApiResponseResult BadRequest(string message, long code = 400)
        => new(code, message, null, false);

    /// <summary>失败信封（HTTP 404）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">错误消息，默认「资源不存在」。</param>
    public static ApiResponseResult NotFound(string message = "资源不存在")
        => new(404, message, null, false);

    /// <summary>失败信封（HTTP 401）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">错误消息，默认「未授权访问」。</param>
    public static ApiResponseResult Unauthorized(string message = "未授权访问")
        => new(401, message, null, false);

    /// <summary>失败信封（HTTP 403）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">错误消息，默认「禁止访问」。</param>
    public static ApiResponseResult Forbidden(string message = "禁止访问")
        => new(403, message, null, false);

    /// <summary>失败信封（HTTP 500）——兼容旧 Message.ApiResponse 工厂签名。</summary>
    /// <param name="message">错误消息。</param>
    /// <param name="code">HTTP 状态码，默认 500。</param>
    public static ApiResponseResult Error(string message, long code = 500)
        => new(code, message, null, false);
}
