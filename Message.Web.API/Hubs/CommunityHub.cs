
namespace Message.Web.API.Hubs;

/// <summary>
/// 兴趣社区实时通信 Hub。
/// <para>
/// 核心职责：
/// - <b>圈子频道订阅</b>：<see cref="JoinCircle"/>/<see cref="LeaveCircle"/> 将连接加入/移出
///   <c>circle:{id}</c> 群组，成员实时接收圈子内新帖、评论、点赞、成员变动等事件；
/// - 认证：<c>[Authorize]</c> 强制 JWT 认证，用户身份从 sub/NameIdentifier/user_guid Claim 解析（与 MessageHub 一致）。
/// </para>
/// </summary>
[Authorize]
public class CommunityHub : Hub<ICommunityClient>
{
    private readonly ICircleRepository _circleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CommunityHub> _logger;

    public CommunityHub(
        ICircleRepository circleRepository,
        ICurrentUserService currentUserService,
        ILogger<CommunityHub> logger)
    {
        _circleRepository = circleRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>圈子对应的 SignalR 群组名</summary>
    public static string CircleGroupName(Guid circleGuid) => $"circle:{circleGuid}";

    /// <summary>
    /// 订阅圈子频道（仅圈子成员可订阅，服务端强校验）。
    /// </summary>
    public async Task JoinCircle(Guid circleGuid)
    {
        var userId = GetUserId();

        if (!await _circleRepository.IsMemberAsync(circleGuid, userId))
        {
            _logger.LogWarning("非圈子成员尝试订阅频道: Circle={CircleGuid}, User={UserGuid}", circleGuid, userId);
            throw new HubException("不是圈子成员，无法订阅");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, CircleGroupName(circleGuid));
        _logger.LogInformation("用户订阅圈子频道: Circle={CircleGuid}, User={UserGuid}", circleGuid, userId);
    }

    /// <summary>
    /// 取消订阅圈子频道。
    /// </summary>
    public Task LeaveCircle(Guid circleGuid)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, CircleGroupName(circleGuid));
    }

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value
                          ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? Context.User?.FindFirst("user_guid")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        try
        {
            return _currentUserService.GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("无法从连接 {ConnectionId} 解析用户标识", Context.ConnectionId);
            throw new HubException("无效的用户标识");
        }
    }
}
