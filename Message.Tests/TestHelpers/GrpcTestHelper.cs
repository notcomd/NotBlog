using Grpc.Core;

namespace Message.Tests.TestHelpers;

/// <summary>
/// gRPC 测试辅助工具：构造 <see cref="AsyncUnaryCall{T}"/>，用于 Mock 生成的 gRPC 客户端方法。
/// </summary>
internal static class GrpcTestHelper
{
    /// <summary>构造成功的单次一元调用结果</summary>
    public static AsyncUnaryCall<T> Success<T>(T response) => new(
        Task.FromResult(response),
        _ => Task.FromResult(new Metadata()),
        _ => Status.DefaultSuccess,
        _ => new Metadata(),
        _ => { },
        CancellationToken.None);

    /// <summary>构造抛出 <see cref="RpcException"/> 的调用结果（用于测试异常处理与重试）</summary>
    public static AsyncUnaryCall<T> Throws<T>(RpcException exception) => new(
        Task.FromException<T>(exception),
        _ => Task.FromResult(new Metadata()),
        _ => exception.Status,
        _ => new Metadata(),
        _ => { },
        CancellationToken.None);

    /// <summary>构造一个指定状态码的 <see cref="RpcException"/></summary>
    public static RpcException RpcError(StatusCode statusCode, string detail = "grpc 服务异常")
        => new(new Status(statusCode, detail));
}
