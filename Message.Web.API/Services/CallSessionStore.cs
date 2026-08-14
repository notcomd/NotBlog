namespace Message.Web.API.Services;

/// <summary>
/// 通话会话存储与状态机（Singleton，跨实例安全）。
/// <para>
/// 存储：通话状态（<see cref="CallSession"/>）序列化后存 Redis（TTL 6 小时兜底），
/// 用户 → 当前通话的映射存 <c>message:call:user:{userId}</c>，用于忙线判定与断线清理。
/// </para>
/// <para>
/// 状态流转：
/// <list type="bullet">
/// <item><b>Ringing</b>：呼叫方 <see cref="CreateCallAsync"/> 创建，向在线被叫推送 IncomingCall；
/// 30 秒无人应答由 <see cref="EnsureNotTimedOutAsync"/> 惰性终结（Timeout），客户端也可主动取消；</item>
/// <item><b>Active</b>：首名被叫接通（<see cref="AcceptCallAsync"/>），呼叫方与其同时进入通话，
/// 广播 CallStarted；后续成员可 <see cref="JoinCallAsync"/> 加入（群组 Mesh 拓扑）；</item>
/// <item><b>Ended</b>：取消 / 拒绝 / 超时 / 全员离开 / 1 对 1 对方挂断，广播 CallEnded 后清理。</item>
/// </list>
/// 1 对 1 判定：会话参与者恰为 2 人（<see cref="ChatSession"/> 的私聊会话）。
/// </para>
/// <para>
/// 推送：经 <see cref="IHubContext{CallHub, ICallClient}"/> 按 Redis 连接管理器的在线连接推送
/// （与 MessageDeliveryService 同源，跨实例无重复）。连接查询通过 <see cref="IServiceScopeFactory"/>
/// 解析 Scoped 的 <see cref="IConnectionManager"/>（本类为 Singleton，禁止直接注入 Scoped 服务）。
/// </para>
/// </summary>
public sealed class CallSessionStore
{
    private const string CallKeyPrefix = "message:call:";
    private const string UserCallKeyPrefix = "message:call:user:";

    /// <summary>通话状态兜底 TTL（防止 Redis 残留泄漏；正常结束会主动清理）</summary>
    private static readonly TimeSpan CallTtl = TimeSpan.FromHours(6);

    /// <summary>响铃超时：超时无人应答由服务端惰性终结</summary>
    private static readonly TimeSpan RingingTimeout = TimeSpan.FromSeconds(30);

    private readonly RedisCacheService _cache;
    private readonly IHubContext<CallHub, ICallClient> _hub;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CallSessionStore> _logger;

    public CallSessionStore(
        RedisCacheService cache,
        IHubContext<CallHub, ICallClient> hub,
        IServiceScopeFactory scopeFactory,
        ILogger<CallSessionStore> logger)
    {
        _cache = cache;
        _hub = hub;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════
    // 公开操作
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// 发起呼叫：创建 Ringing 通话，向在线且不忙的被叫推送 <see cref="ICallClient.IncomingCall"/>。
    /// 呼叫方立即标记为"通话中"（忙线）。
    /// </summary>
    /// <exception cref="InvalidOperationException">呼叫方已在通话中</exception>
    public async Task<CallStartResult> CreateCallAsync(
        Guid sessionId, Guid callerId, CallType type,
        IReadOnlyCollection<Guid> participants, CancellationToken ct)
    {
        var callerCallId = await GetUserCallIdAsync(callerId, ct);
        if (callerCallId is not null)
            throw new InvalidOperationException("您已在通话中，无法发起新的呼叫");

        var callId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var busyUsers = new List<Guid>();
        var callable = new List<Guid>();
        foreach (var userId in participants.Where(p => p != callerId))
        {
            var busyWith = await GetUserCallIdAsync(userId, ct);
            if (busyWith is not null)
                busyUsers.Add(userId);
            else
                callable.Add(userId);
        }

        var session = new CallSession
        {
            CallId = callId,
            SessionId = sessionId,
            Type = type,
            Status = CallStatus.Ringing,
            CallerId = callerId,
            CreatedAt = now,
            Members = participants.ToDictionary(p => p, _ => CallMemberState.Pending)
        };
        session.BusyUsers.AddRange(busyUsers);

        await SaveAsync(session, ct);
        await SetUserCallAsync(callerId, callId, ct);

        // 向在线且不忙的被叫推送来电（离线成员记录在结果中由 UI 提示；
        // 连接查询/推送失败按离线处理并记日志，不影响呼叫创建）
        var offlineUsers = new List<Guid>();
        foreach (var userId in callable)
        {
            IReadOnlyList<string> connections;
            try
            {
                connections = await GetConnectionsAsync(userId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Call] 查询被叫连接失败，按离线处理 UserId={UserId}", userId);
                offlineUsers.Add(userId);
                continue;
            }

            if (connections.Count == 0)
            {
                offlineUsers.Add(userId);
                continue;
            }

            var callInfo = session.ToDto();
            try
            {
                await Task.WhenAll(connections.Select(c =>
                    _hub.Clients.Client(c).IncomingCall(callInfo)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Call] 推送来电失败 UserId={UserId}, CallId={CallId}", userId, callId);
            }
        }

        _logger.LogInformation(
            "[Call] 发起呼叫 CallId={CallId}, SessionId={SessionId}, Type={Type}, Caller={Caller}, " +
            "Busy={BusyCount}, Offline={OfflineCount}",
            callId, sessionId, type, callerId, busyUsers.Count, offlineUsers.Count);

        return new CallStartResult
        {
            CallId = callId,
            SessionId = sessionId,
            Type = type,
            CallerId = callerId,
            Participants = participants.ToList(),
            BusyUsers = busyUsers,
            OfflineUsers = offlineUsers,
            CreatedAt = now
        };
    }

    /// <summary>
    /// 取消呼叫（仅呼叫方，响铃阶段）。向所有收到来电的被叫推送 <see cref="ICallClient.CallEnded"/>
    /// （reason = CallerCancelled）。
    /// </summary>
    public async Task<CallInfoDto?> CancelCallAsync(Guid callId, Guid userId, CancellationToken ct)
    {
        var session = await LoadAsync(callId, ct);
        if (session is null)
            return null;

        if (session.CallerId != userId)
            throw new InvalidOperationException("只有呼叫方可以取消呼叫");
        if (session.Status != CallStatus.Ringing)
            throw new InvalidOperationException("通话已建立，请使用挂断结束通话");

        await EndCallAsync(session, CallEndReason.CallerCancelled, ct);
        return session.ToDto();
    }

    /// <summary>
    /// 接听 / 加入通话。响铃阶段首次接听将通话置为 Active 并广播
    /// <see cref="ICallClient.CallStarted"/>（呼叫方与接通成员互见快照）；
    /// 通话中阶段（群组/重连）加入广播 <see cref="ICallClient.MemberJoined"/>。
    /// </summary>
    public async Task<CallInfoDto?> AcceptCallAsync(Guid callId, Guid userId, CancellationToken ct)
    {
        var session = await LoadAsync(callId, ct);
        if (session is null)
            return null;
        if (!session.Members.ContainsKey(userId))
            throw new InvalidOperationException("您不是该通话的成员");

        // 响铃超时兜底：超时后不接受（客户端应收到 Timeout 结束事件）
        if (!await EnsureNotTimedOutAsync(session, ct))
            return null;

        var wasActive = session.Status == CallStatus.Active;
        if (!wasActive)
        {
            // 首名成员接通：通话建立（呼叫方同步进入通话）
            session.Status = CallStatus.Active;
            session.ConnectedAt = DateTimeOffset.UtcNow;
            session.Members[session.CallerId] = CallMemberState.Joined;
        }

        session.Members[userId] = CallMemberState.Joined;
        await SaveAsync(session, ct);
        await SetUserCallAsync(userId, callId, ct);

        var callInfo = session.ToDto();

        if (!wasActive)
        {
            // 通话建立：通知呼叫方（及其全部在线连接）
            await PushToUserAsync(session.CallerId,
                c => c.CallStarted(callInfo), ct);
        }

        // 通知通话内其他已接通成员：新成员加入
        await PushToJoinedOthersAsync(session, userId,
            c => c.MemberJoined(callInfo, userId), ct);

        _logger.LogInformation(
            "[Call] 成员接通 CallId={CallId}, UserId={UserId}, WasActive={WasActive}, Joined={JoinedCount}",
            callId, userId, wasActive, callInfo.JoinedMembers.Count);

        return callInfo;
    }

    /// <summary>
    /// 拒绝来电（响铃阶段）。
    /// 1 对 1 通话：拒绝即通话结束（reason = Rejected）；群组通话：仅通知呼叫方 MemberRejected。
    /// </summary>
    public async Task<CallInfoDto?> RejectCallAsync(Guid callId, Guid userId, CancellationToken ct)
    {
        var session = await LoadAsync(callId, ct);
        if (session is null)
            return null;
        if (!session.Members.ContainsKey(userId))
            throw new InvalidOperationException("您不是该通话的成员");
        if (session.Status != CallStatus.Ringing)
            return session.ToDto();

        session.Members[userId] = CallMemberState.Rejected;

        if (IsOneToOne(session))
        {
            await EndCallAsync(session, CallEndReason.Rejected, ct);
        }
        else
        {
            await SaveAsync(session, ct);
            await PushToUserAsync(session.CallerId,
                c => c.MemberRejected(session.ToDto(), userId), ct);
            _logger.LogInformation("[Call] 成员拒绝来电 CallId={CallId}, UserId={UserId}", callId, userId);
        }

        return session.ToDto();
    }

    /// <summary>
    /// 挂断 / 离开通话。
    /// 1 对 1：通话结束（对方收到 reason = RemoteHangup）；
    /// 群组：广播 MemberLeft，最后一名成员离开时通话结束（reason = AllLeft）；
    /// 响铃阶段由呼叫方调用等价于取消，由被叫调用等价于拒绝。
    /// </summary>
    public async Task<CallInfoDto?> HangUpAsync(Guid callId, Guid userId, CancellationToken ct)
    {
        var session = await LoadAsync(callId, ct);
        if (session is null)
            return null;
        if (!session.Members.ContainsKey(userId))
            throw new InvalidOperationException("您不是该通话的成员");

        // 响铃阶段：呼叫方挂断 = 取消；被叫挂断 = 拒绝
        if (session.Status == CallStatus.Ringing)
        {
            if (session.CallerId == userId)
                return await CancelCallAsync(callId, userId, ct);
            return await RejectCallAsync(callId, userId, ct);
        }

        if (session.Status != CallStatus.Active)
            return session.ToDto();

        await RemoveUserCallAsync(userId, ct);

        if (IsOneToOne(session))
        {
            await EndCallAsync(session, CallEndReason.RemoteHangup, ct);
            return session.ToDto();
        }

        // 群组：成员离开
        session.Members[userId] = CallMemberState.Left;
        var remainingJoined = session.Members.Count(kv => kv.Value == CallMemberState.Joined);

        if (remainingJoined == 0)
        {
            await EndCallAsync(session, CallEndReason.AllLeft, ct);
        }
        else
        {
            await SaveAsync(session, ct);
            await PushToJoinedOthersAsync(session, userId,
                c => c.MemberLeft(session.ToDto(), userId), ct);
            _logger.LogInformation(
                "[Call] 成员离开通话 CallId={CallId}, UserId={UserId}, Remaining={Remaining}",
                callId, userId, remainingJoined);
        }

        return session.ToDto();
    }

    /// <summary>
    /// 查询通话状态（不存在或已清理返回 null）。附带响铃超时惰性终结。
    /// </summary>
    public async Task<CallInfoDto?> GetCallAsync(Guid callId, CancellationToken ct)
    {
        var session = await LoadAsync(callId, ct);
        if (session is null)
            return null;

        await EnsureNotTimedOutAsync(session, ct);
        return session.ToDto();
    }

    /// <summary>
    /// 转发 WebRTC 信令（offer / answer / ice）。
    /// <para>定向（ToUserId 非空）转发给目标用户全部在线连接；广播转发给通话内其他已接通成员。</para>
    /// </summary>
    public async Task ForwardSignalAsync(CallSignalDto signal, CancellationToken ct)
    {
        var session = await LoadAsync(signal.CallId, ct);
        if (session is null)
            throw new InvalidOperationException("通话不存在或已结束");
        if (session.Status != CallStatus.Active)
            throw new InvalidOperationException("通话尚未建立，无法交换信令");
        if (session.Members.GetValueOrDefault(signal.FromUserId) != CallMemberState.Joined)
            throw new InvalidOperationException("您尚未进入通话，无法发送信令");

        if (signal.ToUserId is { } targetId)
        {
            await PushToUserAsync(targetId, c => c.Signal(signal), ct);
        }
        else
        {
            await PushToJoinedOthersAsync(session, signal.FromUserId,
                c => c.Signal(signal), ct);
        }
    }

    /// <summary>
    /// 连接断开清理：若用户已无任何在线连接且处于通话中，自动将其移出通话
    /// （1 对 1 通话因此结束；群组通话广播 MemberLeft）。
    /// </summary>
    public async Task LeaveCallForDisconnectAsync(Guid userId, bool hasOtherConnections, CancellationToken ct)
    {
        if (hasOtherConnections)
            return;

        string? callId;
        try
        {
            callId = await GetUserCallIdAsync(userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Call] 断线查询通话失败 UserId={UserId}", userId);
            return;
        }

        if (callId is null || !Guid.TryParse(callId, out var guid))
            return;

        try
        {
            await HangUpAsync(guid, userId, ct);
            _logger.LogInformation("[Call] 用户断线离开通话 UserId={UserId}, CallId={CallId}", userId, guid);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Call] 断线离开通话失败 UserId={UserId}, CallId={CallId}", userId, guid);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 私有辅助
    // ═══════════════════════════════════════════════════════

    private bool IsOneToOne(CallSession session) => session.Members.Count == 2;

    private async Task<CallSession?> LoadAsync(Guid callId, CancellationToken ct)
    {
        var session = await _cache.GetAsync<CallSession>(CallKey(callId), ct);
        if (session is null)
            return null;

        // 状态兜底：若 TTL 内残留 Ended 状态（极端情况），按已结束处理
        if (session.Status == CallStatus.Ended)
        {
            await RemoveCallKeysAsync(session, ct);
            return null;
        }

        return session;
    }

    private Task SaveAsync(CallSession session, CancellationToken ct)
        => _cache.SetAsync(CallKey(session.CallId), session, CallTtl, ct);

    private async Task SetUserCallAsync(Guid userId, Guid callId, CancellationToken ct)
        => await _cache.SetAsync(UserCallKey(userId), callId.ToString(), CallTtl, ct);

    private async Task<string?> GetUserCallIdAsync(Guid userId, CancellationToken ct)
        => await _cache.GetAsync<string>(UserCallKey(userId), ct);

    private Task RemoveUserCallAsync(Guid userId, CancellationToken ct)
        => _cache.RemoveAsync(UserCallKey(userId), ct);

    /// <summary>
    /// 终结通话：状态置 Ended → 清理用户映射 → 广播 CallEnded → 删除会话。
    /// </summary>
    private async Task EndCallAsync(CallSession session, CallEndReason reason, CancellationToken ct)
    {
        session.Status = CallStatus.Ended;
        await RemoveCallKeysAsync(session, ct);

        var callInfo = session.ToDto();
        var members = session.Members.Keys.ToArray();

        // 广播给全部参与者（含呼叫方；离线者自然收不到）
        foreach (var userId in members)
        {
            await PushToUserAsync(userId, c => c.CallEnded(callInfo, reason), ct);
        }

        _logger.LogInformation(
            "[Call] 通话结束 CallId={CallId}, Reason={Reason}, Participants={Count}",
            session.CallId, reason, members.Length);
    }

    private async Task RemoveCallKeysAsync(CallSession session, CancellationToken ct)
    {
        foreach (var userId in session.Members.Keys)
            await RemoveUserCallAsync(userId, ct);
        await _cache.RemoveAsync(CallKey(session.CallId), ct);
    }

    /// <summary>
    /// 响铃超时惰性终结：Ringing 状态超过 <see cref="RingingTimeout"/> 即结束（reason = Timeout）。
    /// </summary>
    /// <returns>通话是否仍有效（未超时结束）</returns>
    private async Task<bool> EnsureNotTimedOutAsync(CallSession session, CancellationToken ct)
    {
        if (session.Status != CallStatus.Ringing)
            return true;
        if (DateTimeOffset.UtcNow - session.CreatedAt <= RingingTimeout)
            return true;

        _logger.LogInformation("[Call] 响铃超时 CallId={CallId}, CreatedAt={CreatedAt}",
            session.CallId, session.CreatedAt);
        await EndCallAsync(session, CallEndReason.Timeout, ct);
        return false;
    }

    /// <summary>解析用户在线连接（Scoped 依赖经作用域工厂解析）</summary>
    private async Task<IReadOnlyList<string>> GetConnectionsAsync(Guid userId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var connectionManager = scope.ServiceProvider.GetRequiredService<IConnectionManager>();
        var connections = await connectionManager.GetConnectionsAsync(userId);
        return connections as IReadOnlyList<string> ?? connections.ToArray();
    }

    /// <summary>向指定用户的全部在线连接推送事件（连接查询/推送失败仅记日志，不影响状态机）</summary>
    private async Task PushToUserAsync(Guid userId, Func<ICallClient, Task> send, CancellationToken ct)
    {
        IReadOnlyList<string> connections;
        try
        {
            connections = await GetConnectionsAsync(userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Call] 查询用户连接失败，跳过推送 UserId={UserId}", userId);
            return;
        }

        if (connections.Count == 0)
            return;

        var client = _hub.Clients;
        try
        {
            await Task.WhenAll(connections.Select(c => send(client.Client(c))));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[Call] 推送事件失败 UserId={UserId}, ConnectionCount={Count}", userId, connections.Count);
        }
    }

    /// <summary>向通话内其他已接通成员推送事件（排除指定用户）</summary>
    private async Task PushToJoinedOthersAsync(
        CallSession session, Guid excludeUserId, Func<ICallClient, Task> send, CancellationToken ct)
    {
        var targets = session.Members
            .Where(kv => kv.Key != excludeUserId && kv.Value == CallMemberState.Joined)
            .Select(kv => kv.Key)
            .ToArray();

        foreach (var userId in targets)
            await PushToUserAsync(userId, send, ct);
    }

    private static string CallKey(Guid callId) => $"{CallKeyPrefix}{callId}";
    private static string UserCallKey(Guid userId) => $"{UserCallKeyPrefix}{userId}";
}

/// <summary>
/// 通话会话状态（Redis 持久化模型，<see cref="CallSessionStore"/> 内部使用）。
/// </summary>
public sealed class CallSession
{
    public Guid CallId { get; init; }

    public Guid SessionId { get; init; }

    public CallType Type { get; init; }

    public CallStatus Status { get; set; }

    public Guid CallerId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? ConnectedAt { get; set; }

    /// <summary>会话参与者 → 成员状态（含呼叫方）</summary>
    public Dictionary<Guid, CallMemberState> Members { get; init; } = [];

    /// <summary>发起呼叫时检测到的忙线成员</summary>
    public List<Guid> BusyUsers { get; init; } = [];

    /// <summary>映射为客户端 DTO</summary>
    public CallInfoDto ToDto() => new()
    {
        CallId = CallId,
        SessionId = SessionId,
        Type = Type,
        Status = Status,
        CallerId = CallerId,
        CreatedAt = CreatedAt,
        ConnectedAt = ConnectedAt,
        Participants = Members.Keys.ToList(),
        JoinedMembers = Members.Where(kv => kv.Value == CallMemberState.Joined).Select(kv => kv.Key).ToList(),
        BusyUsers = BusyUsers.ToList()
    };
}

/// <summary>通话成员状态</summary>
public enum CallMemberState
{
    /// <summary>等待应答（响铃中）</summary>
    Pending = 0,

    /// <summary>已接通（通话中）</summary>
    Joined = 1,

    /// <summary>已拒绝来电</summary>
    Rejected = 2,

    /// <summary>已离开通话</summary>
    Left = 3
}
