namespace Notcomd.EventBus.Rpc;

/// <summary>
/// RPC 请求总线接口（同步请求-响应模式）
/// </summary>
public interface IRequestBus
{
    /// <summary>
    /// 发送请求并等待响应
    /// </summary>
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// 发送请求并等待响应（自定义超时）
    /// </summary>
    Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;
}