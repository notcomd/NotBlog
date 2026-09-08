namespace Commons.Web;

/// <summary>
/// 统一响应包装中间件（<see cref="ApiResponseWrappingMiddleware"/>）的配置项。
/// <para>
/// 仅对路径前缀匹配的请求启用包装；其余请求（SignalR、健康检查、OpenAPI、文件下载、gRPC 等）原样透传。
/// </para>
/// </summary>
public sealed class ApiResponseWrappingOptions
{
    /// <summary>
    /// 需要包装的路径前缀集合（默认仅 <c>/api</c>）。
    /// 命中任意前缀即生效；匹配为大小写不敏感的前缀比对。
    /// </summary>
    public IReadOnlyList<string> PathPrefixes { get; set; } = ["/api"];
}
