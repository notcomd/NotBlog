namespace Commons.Result;

/// <summary>
/// 泛型统一 API 响应信封（带业务数据）。
/// <para>
/// 与 <see cref="ApiResponseResult"/> 共同构成全站统一的 HTTP JSON 响应形状：
/// <c>{ statusCode, message, responseData, isSuccess, responseDateTime }</c>（默认 camelCase 序列化）。
/// 端点显式返回本类型实例时，<see cref="Web.ApiResponseWrappingMiddleware"/> 通过透传约定直接放行，避免二次包装。
/// </para>
/// </summary>
/// <typeparam name="TResponse">业务数据类型。</typeparam>
public sealed class ApiResponseResult<TResponse>
{
    /// <summary>HTTP 状态码（与真实响应状态码保持一致）。</summary>
    public long StatusCode { get; }

    /// <summary>面向客户端的提示消息。</summary>
    public string Message { get; }

    /// <summary>业务数据载荷。</summary>
    public TResponse? ResponseData { get; }

    /// <summary>是否成功（HTTP 2xx 为 true，其余为 false）。</summary>
    public bool IsSuccess { get; }

    /// <summary>响应生成时间（UTC）。</summary>
    public DateTime ResponseDateTime { get; }

    /// <summary>私有构造：统一经工厂创建，保证 <see cref="IsSuccess"/> 与 <see cref="StatusCode"/> 语义一致。</summary>
    private ApiResponseResult(long statusCode, string message, TResponse? responseData, bool isSuccess)
    {
        StatusCode = statusCode;
        Message = message;
        ResponseData = responseData;
        IsSuccess = isSuccess;
        ResponseDateTime = DateTime.UtcNow;
    }

    /// <summary>成功信封（HTTP 200）。</summary>
    /// <param name="data">业务数据。</param>
    /// <param name="message">提示消息；为 null 时使用默认文案「请求成功」。</param>
    /// <param name="statusCode">HTTP 状态码，默认 200。</param>
    public static ApiResponseResult<TResponse> Success(TResponse data, string? message = null, long statusCode = 200)
        => new(statusCode, message ?? "请求成功", data, true);

    /// <summary>失败信封（默认 HTTP 400），供错误路径显式返回领域错误消息。</summary>
    /// <param name="message">领域错误消息。</param>
    /// <param name="statusCode">HTTP 状态码，默认 400。</param>
    /// <param name="responseData">可选的附加错误信息载荷。</param>
    public static ApiResponseResult<TResponse> Failure(string message, long statusCode = 400, TResponse? responseData = default)
        => new(statusCode, message, responseData, false);

    /// <summary>成功信封（HTTP 200）——兼容旧 Message/Markdown.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="data">业务数据。</param>
    /// <param name="message">提示消息。</param>
    public static ApiResponseResult<TResponse> Ok(TResponse data, string? message = null)
        => new(200, message ?? "请求成功", data, true);

    /// <summary>成功信封（HTTP 201）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="data">新创建的资源数据。</param>
    /// <param name="message">提示消息；为 null 时使用默认文案「创建成功」。</param>
    public static ApiResponseResult<TResponse> Created(TResponse data, string? message = null)
        => new(201, message ?? "创建成功", data, true);

    /// <summary>失败信封（HTTP 400）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="message">错误消息。</param>
    /// <param name="code">HTTP 状态码，默认 400。</param>
    public static ApiResponseResult<TResponse> BadRequest(string message, long code = 400)
        => new(code, message, default, false);

    /// <summary>失败信封（HTTP 404）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="message">错误消息，默认「资源不存在」。</param>
    public static ApiResponseResult<TResponse> NotFound(string message = "资源不存在")
        => new(404, message, default, false);

    /// <summary>失败信封（HTTP 401）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="message">错误消息，默认「未授权访问」。</param>
    public static ApiResponseResult<TResponse> Unauthorized(string message = "未授权访问")
        => new(401, message, default, false);

    /// <summary>失败信封（HTTP 403）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="message">错误消息，默认「禁止访问」。</param>
    public static ApiResponseResult<TResponse> Forbidden(string message = "禁止访问")
        => new(403, message, default, false);

    /// <summary>失败信封（HTTP 500）——兼容旧 Message.ApiResponse&lt;T&gt; 工厂签名。</summary>
    /// <param name="message">错误消息。</param>
    /// <param name="code">HTTP 状态码，默认 500。</param>
    public static ApiResponseResult<TResponse> Error(string message, long code = 500)
        => new(code, message, default, false);
}
