using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Commons.Web;

/// <summary>
/// 统一 API 响应包装中间件。
/// <para>
/// 将下游端点写出的 JSON 响应体自动包装为 <see cref="Result.ApiResponseResult"/> 形状
/// （<c>{ statusCode, message, responseData, isSuccess, responseDateTime }</c>），
/// 使全站 HTTP API 的返回格式统一。必须注册在 <see cref="ExceptionSanitizingMiddleware"/> 之后
/// （异常由外层脱敏中间件直接输出统一信封，不经本中间件）。
/// </para>
/// <para>
/// 处理规则：
/// <list type="number">
/// <item>仅包装 <see cref="ApiResponseWrappingOptions.PathPrefixes"/> 匹配且 Content-Type 为 application/json 的响应；</item>
/// <item>透传约定：下游端点显式返回了统一信封（根对象同时含 isSuccess / statusCode / responseData 三属性）时原样放行，避免二次包装；</item>
/// <item>204/304、HEAD/OPTIONS、空响应体（非错误状态码）与非 JSON 响应（流式下载/视频流等）一律原样透传；</item>
/// <item>错误状态码但响应体为空的（如 JWT 401 challenge）注入默认失败信封，保证前端能读到统一错误体。</item>
/// </list>
/// </para>
/// </summary>
public sealed class ApiResponseWrappingMiddleware(
    RequestDelegate next,
    IOptions<ApiResponseWrappingOptions> options)
{
    private readonly IReadOnlyList<string> _pathPrefixes = options.Value.PathPrefixes;

    /// <summary>请求是否命中需要包装的路径前缀。</summary>
    private bool IsWrappablePath(PathString path)
    {
        foreach (var prefix in _pathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>根据 HTTP 状态码给出默认提示文案。</summary>
    private static string DefaultMessage(int statusCode) => statusCode switch
    {
        200 => "请求成功",
        201 => "创建成功",
        202 => "请求已接受",
        400 => "请求失败",
        401 => "未授权访问",
        403 => "禁止访问",
        404 => "资源不存在",
        408 => "请求超时",
        409 => "请求冲突",
        413 => "请求体过大",
        422 => "请求无法处理",
        429 => "请求过于频繁",
        500 => "服务器内部错误",
        502 => "网关错误",
        503 => "服务不可用",
        504 => "网关超时",
        >= 200 and < 300 => "请求成功",
        >= 400 and < 500 => "请求失败",
        _ => "服务器内部错误"
    };

    /// <summary>判断响应体是否已是统一信封（根对象含三属性即视为已包装，透传放行）。</summary>
    private static bool IsUnifiedEnvelope(in JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("isSuccess", out _)
        && root.TryGetProperty("statusCode", out _)
        && root.TryGetProperty("responseData", out _);

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsWrappablePath(context.Request.Path))
        {
            await next(context);
            return;
        }

        // 缓冲下游响应体；异常不在此捕获，继续上抛给外层 ExceptionSanitizingMiddleware 统一处理
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        await WrapOrPassthroughAsync(context, buffer);
    }

    /// <summary>按规则将缓冲的响应体改写为统一信封或原样写回。</summary>
    private static async Task WrapOrPassthroughAsync(HttpContext context, MemoryStream buffer)
    {
        var response = context.Response;

        // 响应已开始写出（含认证中间件直接写入头的情况）无法改写，原样透传
        if (response.HasStarted
            || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method)
            || response.StatusCode is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified)
        {
            await WriteBufferAsync(response, buffer);
            return;
        }

        // 非 application/json（流式下载/视频流/multipart 等）原样透传
        var contentType = response.ContentType;
        if (!contentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            await WriteBufferAsync(response, buffer);
            return;
        }

        // 空响应体：错误状态码注入默认失败信封；其余（如空 2xx）原样透传
        if (buffer.Length == 0)
        {
            if (response.StatusCode < StatusCodes.Status400BadRequest)
            {
                return;
            }

            response.ContentLength = null;
            await response.WriteAsJsonAsync(new
            {
                statusCode = (long)response.StatusCode,
                message = DefaultMessage(response.StatusCode),
                responseData = (object?)null,
                isSuccess = false,
                responseDateTime = DateTime.UtcNow
            });
            return;
        }

        // 解析 JSON；失败视为不可包装内容原样透传（fail-safe，不制造 500）
        JsonDocument document;
        try
        {
            var segment = new ReadOnlyMemory<byte>(buffer.GetBuffer(), 0, (int)buffer.Length);
            document = JsonDocument.Parse(segment);
        }
        catch (JsonException)
        {
            await WriteBufferAsync(response, buffer);
            return;
        }

        using (document)
        {
            // 端点已显式返回统一信封：透传防双层包装
            if (IsUnifiedEnvelope(document.RootElement))
            {
                await WriteBufferAsync(response, buffer);
                return;
            }

            var statusCode = response.StatusCode;
            var isSuccess = statusCode >= StatusCodes.Status200OK && statusCode < StatusCodes.Status300MultipleChoices;
            response.ContentLength = null;
            await response.WriteAsJsonAsync(new
            {
                statusCode = (long)statusCode,
                message = DefaultMessage(statusCode),
                responseData = document.RootElement,
                isSuccess,
                responseDateTime = DateTime.UtcNow
            });
        }
    }

    /// <summary>将缓冲内容原样写回原始响应体。</summary>
    private static async Task WriteBufferAsync(HttpResponse response, MemoryStream buffer)
    {
        if (buffer.Length == 0)
        {
            return;
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(response.Body);
    }
}
