using System.Security.Claims;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT.Core;

namespace FileDev.Web.API.Grpc;

/// <summary>
/// gRPC JWT 认证拦截器（S-08）。
/// <para>
/// 从请求元数据 Authorization 头（Bearer token）解析 JWT 并校验签名/有效期，
/// 通过后将服务端解析出的调用者 id 写入 <see cref="ServerCallContext.UserState"/>，
/// 供服务方法用于归属校验，杜绝伪造客户端传入的 userId。
/// </para>
/// </summary>
public class GrpcJwtAuthInterceptor : Interceptor
{
    /// <summary>UserState 中调用者 id 的键</summary>
    public const string CallerUserIdStateKey = "caller_user_id";

    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOptionsSnapshot<JwtOptions> _jwtOptions;

    public GrpcJwtAuthInterceptor(IJwtTokenService jwtTokenService, IOptionsSnapshot<JwtOptions> jwtOptions)
    {
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions;
    }

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, context);
    }

    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, responseStream, context);
    }

    private void Authenticate(ServerCallContext context)
    {
        var authHeader = context.RequestHeaders.FirstOrDefault(h =>
            string.Equals(h.Key, "authorization", StringComparison.OrdinalIgnoreCase))?.Value;

        var callerId = ResolveCallerId(authHeader);
        if (callerId is null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "未认证或令牌无效"));

        context.UserState[CallerUserIdStateKey] = callerId.Value;
    }

    private Guid? ResolveCallerId(string? authHeader)
    {
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..].Trim()
            : authHeader.Trim();

        if (string.IsNullOrEmpty(token))
            return null;

        var principal = _jwtTokenService.ValidateToken(token, _jwtOptions.Value);
        var claim = principal?.FindFirst("id") ?? principal?.FindFirst(ClaimTypes.NameIdentifier);

        return claim is not null && Guid.TryParse(claim.Value, out var uid) ? uid : null;
    }
}
