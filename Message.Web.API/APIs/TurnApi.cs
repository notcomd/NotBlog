using Message.Domain.IServices;
using Message.Web.API.Dto.Call;

namespace Message.Web.API.APIs;

/// <summary>
/// WebRTC TURN 限时凭证接口。
/// <para>
/// 返回**原始 <see cref="TurnCredentialsDto"/>**（非 <see cref="ApiResponse{T}"/> 外层包裹）：
/// 该接口由浏览器端 <c>getIceServers</c> 直接消费，需严格匹配 <c>{ urls, username, credential }</c> 结构，
/// 不引入业务响应封装，避免前端额外解包。
/// </para>
/// </summary>
public static class TurnApi
{
    /// <summary>映射 TURN 凭证端点组（需登录）</summary>
    public static RouteGroupBuilder MapTurnApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/turn")
            .WithTags("Turn")
            .RequireAuthorization();

        // GET /api/turn/credentials — 获取 WebRTC TURN 限时凭证
        group.MapGet("/credentials", GetCredentialsAsync)
            .WithSummary("获取 TURN 限时凭证")
            .WithDescription("按 coturn use-auth-secret 规范签发限时 username + credential，供 RTCPeerConnection 使用")
            .Produces<TurnCredentialsDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static IResult GetCredentialsAsync(
        [FromServices] ICurrentUserService currentUser,
        [FromServices] TurnCredentialService turnService)
    {
        var credentials = turnService.Generate(currentUser.GetUserId());
        return Results.Ok(credentials);
    }
}