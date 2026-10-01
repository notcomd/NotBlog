using System.Security.Claims;
using FileDev.Infrastructure.Service;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GrpcJwtAuthInterceptor> _logger;

    public GrpcJwtAuthInterceptor(IJwtTokenService jwtTokenService,
                                  IOptionsSnapshot<JwtOptions> jwtOptions,
                                  ILogger<GrpcJwtAuthInterceptor> logger)
    {
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions;
        _logger = logger;
    }

    /// <summary>
    /// 处理 gRPC 服务器单请求，认证 JWT 并将解析出的调用者 id 写入 <see cref="ServerCallContext.UserState"/>。
    /// </summary>
    /// <param name="request">请求消息</param>
    /// <param name="context">gRPC 请求上下文</param>
    /// <param name="continuation">继续处理请求的委托</param>
    /// <returns>任务</returns>
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, context);
    }

    /// <summary>
    /// 处理 gRPC 服务器流式请求，认证 JWT 并将解析出的调用者 id 写入 <see cref="ServerCallContext.UserState"/>。
    /// </summary>
    /// <param name="request">请求消息</param>
    /// <param name="responseStream">响应流</param>
    /// <param name="context">gRPC 请求上下文</param>
    /// <param name="continuation">继续处理请求的委托</param>
    /// <returns>任务</returns>
    public override Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        Authenticate(context);
        return continuation(request, responseStream, context);
    }

    /// <summary>
    /// 认证 gRPC 请求，校验 JWT 并将解析出的调用者 id 写入 <see cref="ServerCallContext.UserState"/>。
    /// </summary>
    /// <param name="context">gRPC 请求上下文</param>
    private void Authenticate(ServerCallContext context)
    {
        var authHeader = context.RequestHeaders.FirstOrDefault(h =>
            string.Equals(h.Key, "authorization", StringComparison.OrdinalIgnoreCase))?.Value;

        var callerId = ResolveCallerId(authHeader);
        if (callerId is null)
        {
            _logger.LogWarning("gRPC 认证失败: Method={Method}, Peer={Peer}",
                context.Method, context.Peer);
            throw new RpcException(new Status(StatusCode.Unauthenticated, "未认证或令牌无效"));
        }

        context.UserState[CallerUserIdStateKey] = callerId.Value;
        // 租户 ID = 当前调用者用户 ID，随异步链传播供存储层命名空间路由
        AsyncLocalTenantContext.SetTenantId(callerId.Value.ToString("N"));
    }

    /// <summary>
    /// 从请求元数据 Authorization 头（Bearer token）解析 JWT 并校验签名/有效期，
    /// 通过后返回服务端解析出的调用者 id。
    /// </summary>
    /// <param name="authHeader">请求元数据 Authorization 头（Bearer token）</param>
    /// <returns>服务端解析出的调用者 id，若解析失败则返回 null</returns>
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
