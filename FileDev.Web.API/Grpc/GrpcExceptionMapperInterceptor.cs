using FileDev.Domain.Exception;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace FileDev.Web.API.Grpc;

/// <summary>
/// gRPC 全局异常拦截器：将业务异常统一映射为 gRPC 状态码，让客户端明确失败原因。
/// <list type="bullet">
/// <item><see cref="NotFileNotFoundException"/> → <see cref="StatusCode.NotFound"/></item>
/// <item><see cref="FilePermissionDeniedException"/> / <see cref="UnauthorizedAccessException"/> → <see cref="StatusCode.PermissionDenied"/></item>
/// <item><see cref="FileQuotaExceededException"/> → <see cref="StatusCode.ResourceExhausted"/></item>
/// <item><see cref="ArgumentException"/> → <see cref="StatusCode.InvalidArgument"/></item>
/// <item><see cref="OperationCanceledException"/> → <see cref="StatusCode.Cancelled"/></item>
/// <item>其余异常 → <see cref="StatusCode.Internal"/>（不向客户端泄露内部细节）</item>
/// </list>
/// 所有映射均携带 CorrelationId（trailers + 日志），便于服务端与客户端联合追踪。
/// </summary>
public class GrpcExceptionMapperInterceptor : Interceptor
{
    private readonly ILogger<GrpcExceptionMapperInterceptor> _logger;

    public GrpcExceptionMapperInterceptor(ILogger<GrpcExceptionMapperInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            throw MapToRpcException(ex, context);
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (Exception ex)
        {
            throw MapToRpcException(ex, context);
        }
    }

    private RpcException MapToRpcException(Exception exception, ServerCallContext context)
    {
        // RpcException（含认证拦截器抛出的 Unauthenticated）原样透传
        if (exception is RpcException rpcException)
            return rpcException;

        var correlationId = context.RequestHeaders.GetValue("x-correlation-id") ?? Guid.NewGuid().ToString("N");

        var (status, message) = exception switch
        {
            NotFileNotFoundException e => (StatusCode.NotFound, e.Message),
            FilePermissionDeniedException e => (StatusCode.PermissionDenied, e.Message),
            FileQuotaExceededException e => (StatusCode.ResourceExhausted, e.Message),
            UnauthorizedAccessException e => (StatusCode.PermissionDenied, "无权执行此操作"),
            ArgumentException e => (StatusCode.InvalidArgument, e.Message),
            OperationCanceledException => (StatusCode.Cancelled, "请求已取消"),
            // 基类 NotFileException 及其他未预期异常：记录完整异常，仅向客户端返回通用信息，避免泄露内部细节
            _ => (StatusCode.Internal, "文件服务内部错误")
        };

        using (_logger.BeginScope(new[] { new KeyValuePair<string, object>("CorrelationId", correlationId) }))
        {
            if (status == StatusCode.Internal)
                _logger.LogError(exception, "gRPC 调用处理失败: Method={Method}, CorrelationId={CorrelationId}",
                    context.Method, correlationId);
            else
                _logger.LogWarning("gRPC 业务异常: Method={Method}, Status={Status}, Message={Message}, CorrelationId={CorrelationId}",
                    context.Method, status, message, correlationId);
        }

        return new RpcException(
            new Status(status, message),
            new Metadata { { "x-correlation-id", correlationId } });
    }
}
