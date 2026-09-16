namespace Message.Web.API.Hubs;

/// <summary>
/// 语音 / 视频通话信令 Hub（WebRTC over SignalR）。
/// <para>
/// 核心职责：
/// - <b>呼叫控制</b>：<see cref="StartCall"/>（基于会话发起语音/视频呼叫）→ 被叫收到
///   <see cref="ICallClient.IncomingCall"/> → <see cref="AcceptCall"/>/<see cref="RejectCall"/>/
///   <see cref="JoinCall"/>（群组加入）→ <see cref="HangUp"/>/<see cref="CancelCall"/>；
/// - <b>信令转发</b>：<see cref="SendSignal"/> 定向/广播转发 WebRTC offer / answer / ICE，
///   服务端不解析媒体内容（Mesh 全网状拓扑，媒体流为 P2P 直连）；
/// - <b>状态管理</b>：通话状态存 Redis（<see cref="CallSessionStore"/>），跨实例一致，
///   忙线判定、响铃超时兜底、断线自动离开；
/// - <b>认证</b>：<c>[Authorize]</c> 强制 JWT，用户身份从 sub/NameIdentifier/user_guid Claim 解析
///   （与 MessageHub 一致）；接入层仅允许会话参与者发起/加入通话。
/// </para>
/// </summary>
[Authorize]
public class CallHub : Hub<ICallClient>
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConnectionCommandService _connectionCommandService;
    private readonly IConnectionManager _connectionManager;
    private readonly CallSessionStore _callStore;
    private readonly ILogger<CallHub> _logger;

    public CallHub(
        IChatSessionRepository sessionRepository,
        ICurrentUserService currentUserService,
        IConnectionCommandService connectionCommandService,
        IConnectionManager connectionManager,
        CallSessionStore callStore,
        ILogger<CallHub> logger)
    {
        _sessionRepository = sessionRepository;
        _currentUserService = currentUserService;
        _connectionCommandService = connectionCommandService;
        _connectionManager = connectionManager;
        _callStore = callStore;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════
    // 呼叫控制
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 基于会话发起语音 / 视频呼叫（1 对 1 与群组会话均支持，通话成员 = 会话参与者）。
    /// <para>
    /// 群组/频道会话为<b>常驻房间</b>：创建即开启，创建者立即入会；<paramref name="targetMemberIds"/>
    /// 圈选被邀请成员（收到来电），为空 = 邀请全部；未选成员仍可经 <see cref="GetSessionRooms"/> 自由加入。
    /// 可设置 <paramref name="password"/> 作为入会密码（仅群组房间允许）。
    /// </para>
    /// </summary>
    /// <param name="sessionId">会话 ID（ChatSession）</param>
    /// <param name="type">通话类型（语音 / 视频）</param>
    /// <param name="targetMemberIds">被邀请成员（可空；仅群组房间生效）</param>
    /// <param name="password">入会密码（可选，仅群组房间允许）</param>
    /// <returns>通话信息（含忙线/离线成员，供 UI 提示）</returns>
    [HubMethodName("StartCall")]
    public async Task<CallStartResult> StartCall(
        Guid sessionId, CallType type,
        IReadOnlyCollection<Guid>? targetMemberIds = null,
        string? password = null)
    {
        var userId = GetUserId();

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new HubException("会话不存在");
            if (session.IsDismissed)
                throw new HubException("会话已解散");
            if (!session.IsParticipant(userId))
                throw new HubException("您不是该会话的参与者");
            if (!IsCallableSession(session.SessionType))
                throw new HubException("该类型会话不支持通话");

            var isRoom = session.SessionType is SessionType.Group or SessionType.Channel;

            // 成员圈选必须属于会话参与者（防越权邀请）
            if (targetMemberIds is { Count: > 0 }
                && targetMemberIds.Any(id => !session.Participants.Contains(id)))
                throw new HubException("被邀请成员不在会话中");

            // 密码仅群组/频道房间允许（私聊为即时呼叫）
            if (!string.IsNullOrWhiteSpace(password) && !isRoom)
                throw new HubException("仅群组房间可设置入会密码");

            return await _callStore.CreateCallAsync(
                sessionId, userId, type, session.Participants,
                targetMemberIds, password, isRoom, Context.ConnectionAborted);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw WrapError("发起呼叫", ex);
        }
    }

    /// <summary>
    /// 呼叫方取消呼叫（响铃阶段）。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    [HubMethodName("CancelCall")]
    public async Task CancelCall(Guid callId)
    {
        var userId = GetUserId();

        try
        {
            await _callStore.CancelCallAsync(callId, userId, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("取消呼叫", ex);
        }
    }

    /// <summary>
    /// 接听来电（1 对 1 或群组首名接通者：通话建立）。
    /// 房间模式：若设置了入会密码，须提供正确 <paramref name="password"/>。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    /// <param name="password">入会密码（可选；仅常驻房间需要）</param>
    /// <returns>通话信息（含 JoinedMembers 快照，客户端据此向各成员发送 offer）</returns>
    [HubMethodName("AcceptCall")]
    public async Task<CallInfoDto?> AcceptCall(Guid callId, string? password = null)
    {
        var userId = GetUserId();

        try
        {
            return await _callStore.AcceptCallAsync(callId, userId, password, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("接听来电", ex);
        }
    }

    /// <summary>
    /// 拒绝来电（1 对 1 通话因此结束；群组通话仅通知呼叫方）。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    [HubMethodName("RejectCall")]
    public async Task RejectCall(Guid callId)
    {
        var userId = GetUserId();

        try
        {
            await _callStore.RejectCallAsync(callId, userId, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("拒绝来电", ex);
        }
    }

    /// <summary>
    /// 加入通话（群组通话中后续成员 / 断线重连恢复 / 常驻房间自由加入）。
    /// 房间模式：若设置了入会密码，须提供正确 <paramref name="password"/>。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    /// <param name="password">入会密码（可选；仅常驻房间需要）</param>
    /// <returns>通话信息（含 JoinedMembers 快照）</returns>
    [HubMethodName("JoinCall")]
    public async Task<CallInfoDto?> JoinCall(Guid callId, string? password = null)
    {
        var userId = GetUserId();

        try
        {
            return await _callStore.AcceptCallAsync(callId, userId, password, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("加入通话", ex);
        }
    }

    /// <summary>
    /// 关闭常驻房间（仅创建者）。房间内全部成员收到 CallEnded（reason = RoomClosed）。
    /// </summary>
    /// <param name="callId">房间通话 ID</param>
    /// <returns>通话信息（终态）</returns>
    [HubMethodName("CloseRoom")]
    public async Task<CallInfoDto?> CloseRoom(Guid callId)
    {
        var userId = GetUserId();

        try
        {
            return await _callStore.CloseRoomAsync(callId, userId, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("关闭房间", ex);
        }
    }

    /// <summary>
    /// 查询会话下的全部活跃房间（供群组成员自由加入；需为会话参与者）。
    /// </summary>
    /// <param name="sessionId">会话 ID</param>
    /// <returns>活跃房间列表（空 = 无进行中的房间）</returns>
    [HubMethodName("GetSessionRooms")]
    public async Task<List<CallInfoDto>> GetSessionRooms(Guid sessionId)
    {
        var userId = GetUserId();

        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session is null)
                throw new HubException("会话不存在");
            if (!session.IsParticipant(userId))
                throw new HubException("您不是该会话的参与者");

            return await _callStore.GetSessionRoomsAsync(sessionId, Context.ConnectionAborted);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw WrapError("查询会话房间", ex);
        }
    }

    /// <summary>
    /// 挂断 / 离开通话。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    [HubMethodName("HangUp")]
    public async Task HangUp(Guid callId)
    {
        var userId = GetUserId();

        try
        {
            await _callStore.HangUpAsync(callId, userId, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("挂断通话", ex);
        }
    }

    /// <summary>
    /// 查询通话状态（断线重连 / UI 恢复用；通话不存在或已结束返回 null）。
    /// </summary>
    /// <param name="callId">通话 ID</param>
    [HubMethodName("GetCall")]
    public async Task<CallInfoDto?> GetCall(Guid callId)
    {
        var userId = GetUserId();

        try
        {
            var call = await _callStore.GetCallAsync(callId, Context.ConnectionAborted);
            if (call is not null && !call.Participants.Contains(userId))
                throw new HubException("您不是该通话的成员");
            return call;
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw WrapError("查询通话", ex);
        }
    }

    // ═══════════════════════════════════════════════════════
    // WebRTC 信令
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 发送 WebRTC 信令（offer / answer / ice）。
    /// <para>
    /// 定向信令（signal.ToUserId 非空）转发给目标成员；广播信令转发给通话内其他已接通成员。
    /// FromUserId 由服务端按当前连接填充，客户端不可伪造。
    /// </para>
    /// </summary>
    /// <param name="signal">信令数据</param>
    [HubMethodName("SendSignal")]
    public async Task SendSignal(CallSignalDto signal)
    {
        var userId = GetUserId();
        if (signal is null)
            throw new HubException("信令数据不能为空");

        try
        {
            // 服务端权威填充发送方，防伪造
            await _callStore.ForwardSignalAsync(new CallSignalDto
            {
                CallId = signal.CallId,
                FromUserId = userId,
                ToUserId = signal.ToUserId,
                Kind = signal.Kind,
                Sdp = signal.Sdp,
                Candidate = signal.Candidate,
                SdpMid = signal.SdpMid,
                SdpMLineIndex = signal.SdpMLineIndex
            }, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            throw WrapError("发送信令", ex);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 连接生命周期
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 连接断开：移除连接登记；若用户已无任何在线连接且处于通话中，自动移出通话
    /// （1 对 1 通话因此结束，群组通话广播 MemberLeft）。
    /// </summary>
    [HubMethodName("OnDisconnected")]
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        try
        {
            await _connectionCommandService.RemoveConnectionAsync(userId, Context.ConnectionId);
            var hasOtherConnections = await _connectionManager.HasOtherConnectionsAsync(userId);
            await _callStore.LeaveCallForDisconnectAsync(userId, hasOtherConnections, Context.ConnectionAborted);

            await base.OnDisconnectedAsync(exception);
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "通话连接断开处理失败: UserId={UserId}, ConnectionId={ConnectionId}",
                userId, Context.ConnectionId);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════
    // 私有辅助
    // ═══════════════════════════════════════════════════════

    /// <summary>允许发起通话的会话类型（排除 AI 会话与匿名会话）</summary>
    private static bool IsCallableSession(SessionType sessionType)
        => sessionType is SessionType.Private or SessionType.Group or SessionType.Channel;

    /// <summary>
    /// 获取当前连接的用户ID（JWT Claim：sub / NameIdentifier / user_guid，兜底 ICurrentUserService）。
    /// </summary>
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

    /// <summary>
    /// 将异常包装为对客户端友好的 <see cref="HubException"/>，并记录错误日志。
    /// </summary>
    private HubException WrapError(string operation, Exception ex)
    {
        if (ex is HubException hubException)
            return hubException;

        var userId = Context.User?.FindFirst("sub")?.Value ?? "unknown";
        _logger.LogError(ex, "SignalR 通话方法 {Operation} 执行失败, UserId={UserId}, ConnectionId={ConnectionId}",
            operation, userId, Context.ConnectionId);

        return new HubException($"操作失败({operation}): {ex.Message}");
    }
}
