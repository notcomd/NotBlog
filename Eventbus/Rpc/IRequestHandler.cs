namespace Notcomd.EventBus.Rpc;

/// <summary>
/// RPC 请求处理器接口
/// </summary>
/// <typeparam name="TRequest">请求类型</typeparam>
/// <typeparam name="TResponse">响应类型</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : class where TResponse : class
{
    /// <summary>
    /// 处理请求并返回响应
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request);
}