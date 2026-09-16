using System.Reflection;
using Message.Domain.IServices;
using Message.Infrastructure.Services;
using Message.Tests.TestHelpers;
using Message.Web.API.Dto.Call;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using CacheMemory.Core;
using Moq;

namespace Message.Tests.Services;

/// <summary>
/// 通话状态机（<see cref="CallSessionStore"/>）单元测试。
/// 覆盖：呼叫创建（在线/忙线/离线）、接听建立、拒绝（1 对 1 结束 / 群组仅通知）、
/// 挂断（1 对 1 结束 / 群组全员离开）、取消、忙线判定、信令转发校验、响铃超时兜底。
/// </summary>
[TestFixture]
public class CallSessionStoreTests
{
    private static readonly Guid CallerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CalleeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Member3Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid SessionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private const string CallerConn = "conn-caller";
    private const string CalleeConn = "conn-callee";

    private Mock<IRedisCacheService> _db = null!;
    private Dictionary<string, string> _store = null!;
    private Dictionary<string, HashSet<string>> _sets = null!;
    private Mock<IConnectionManager> _connectionManager = null!;
    private Mock<ICallClient> _client = null!;
    private CallSessionStore _storeUnderTest = null!;
    private HashSet<Guid> _onlineUsers = null!;

    [SetUp]
    public void Setup()
    {
        _store = new Dictionary<string, string>();
        _sets = new Dictionary<string, HashSet<string>>();
        _db = new Mock<IRedisCacheService>();
        _db.Setup(x => x.StringGetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                _store.TryGetValue(key, out var value) ? value : null);
        _db.Setup(x => x.StringSetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string value, TimeSpan? _, CancellationToken _) =>
            {
                _store[key] = value;
                return true;
            });
        _db.Setup(x => x.KeyDeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => _store.Remove(key));
        // Set 族（会话活跃房间索引）
        _db.Setup(x => x.SetAddAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string value, CancellationToken _) =>
            {
                if (!_sets.TryGetValue(key, out var set))
                {
                    set = new HashSet<string>();
                    _sets[key] = set;
                }
                return set.Add(value);
            });
        _db.Setup(x => x.SetRemoveAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string value, CancellationToken _) =>
                _sets.TryGetValue(key, out var set) && set.Remove(value));
        _db.Setup(x => x.SetMembersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                _sets.TryGetValue(key, out var set) ? set.ToArray() : Array.Empty<string>());

        // 在线连接：默认仅呼叫方在线
        _onlineUsers = new HashSet<Guid> { CallerId };
        _connectionManager = new Mock<IConnectionManager>();
        _connectionManager.Setup(x => x.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => _onlineUsers.Contains(userId)
                ? new[] { userId == CallerId ? CallerConn : CalleeConn }
                : Array.Empty<string>());
        _connectionManager.Setup(x => x.IsUserOnlineAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => _onlineUsers.Contains(userId));

        // SignalR Hub mock
        _client = new Mock<ICallClient>();
        _client.Setup(x => x.IncomingCall(It.IsAny<CallInfoDto>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.CallStarted(It.IsAny<CallInfoDto>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.CallEnded(It.IsAny<CallInfoDto>(), It.IsAny<CallEndReason>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.MemberJoined(It.IsAny<CallInfoDto>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.MemberLeft(It.IsAny<CallInfoDto>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.MemberRejected(It.IsAny<CallInfoDto>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _client.Setup(x => x.Signal(It.IsAny<CallSignalDto>())).Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients<ICallClient>>();
        hubClients.Setup(x => x.Client(It.IsAny<string>())).Returns(_client.Object);
        var hubContext = new Mock<IHubContext<CallHub, ICallClient>>();
        hubContext.Setup(x => x.Clients).Returns(hubClients.Object);

        // 作用域工厂 → IConnectionManager
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IConnectionManager))).Returns(_connectionManager.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(serviceProvider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

        var redisCache = new MessageCacheService(
            _db.Object,
            new Mock<ILogger<MessageCacheService>>().Object);

        _storeUnderTest = new CallSessionStore(
            redisCache,
            hubContext.Object,
            scopeFactory.Object,
            new Mock<ILogger<CallSessionStore>>().Object);
    }

    // ---------- 发起呼叫 ----------

    [Test]
    public async Task CreateCall_被叫在线_推送IncomingCall并返回呼叫结果()
    {
        _onlineUsers.Add(CalleeId);

        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.That(result.CallId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.BusyUsers, Is.Empty);
        Assert.That(result.OfflineUsers, Is.Empty);
        Assert.That(result.Type, Is.EqualTo(CallType.Video));
        _client.Verify(x => x.IncomingCall(It.Is<CallInfoDto>(c => c.CallerId == CallerId)),
            Times.Once);
    }

    [Test]
    public async Task CreateCall_被叫离线_返回离线成员且不推送来电()
    {
        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.That(result.OfflineUsers, Does.Contain(CalleeId));
        _client.Verify(x => x.IncomingCall(It.IsAny<CallInfoDto>()), Times.Never);
    }

    [Test]
    public async Task CreateCall_呼叫方已在通话中_抛出异常()
    {
        await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.CreateCallAsync(
                SessionId, CallerId, CallType.Video, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None));
    }

    [Test]
    public async Task CreateCall_被叫忙线_标记忙线且不推送来电()
    {
        // 被叫已处于另一个通话中（他作为呼叫方先建一个通话）
        var other = Guid.Parse("00000000-0000-0000-0000-000000000099");
        await _storeUnderTest.CreateCallAsync(
            SessionId, CalleeId, CallType.Audio, new[] { CalleeId, other }, null, null, false, CancellationToken.None);

        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.That(result.BusyUsers, Does.Contain(CalleeId));
        _client.Verify(x => x.IncomingCall(It.IsAny<CallInfoDto>()), Times.Never);
    }

    // ---------- 接听 ----------

    [Test]
    public async Task AcceptCall_首名接通_通话转为Active且呼叫方收到CallStarted()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        var call = await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);

        Assert.That(call, Is.Not.Null);
        Assert.That(call!.Status, Is.EqualTo(CallStatus.Active));
        Assert.That(call.JoinedMembers, Does.Contain(CallerId));
        Assert.That(call.JoinedMembers, Does.Contain(CalleeId));
        _client.Verify(x => x.CallStarted(It.Is<CallInfoDto>(c => c.CallId == created.CallId)),
            Times.Once);
    }

    [Test]
    public async Task AcceptCall_非通话成员_抛出异常()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.AcceptCallAsync(created.CallId, Member3Id, null, CancellationToken.None));
    }

    [Test]
    public async Task AcceptCall_群组后续成员加入_广播MemberJoined()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, false, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);
        _client.Invocations.Clear();

        var call = await _storeUnderTest.AcceptCallAsync(created.CallId, Member3Id, null, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Active));
        Assert.That(call.JoinedMembers, Does.Contain(Member3Id));
        _client.Verify(x => x.MemberJoined(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), Member3Id), Times.AtLeastOnce);
    }

    // ---------- 拒绝 ----------

    [Test]
    public async Task RejectCall_1对1通话_通话结束且原因为Rejected()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        var call = await _storeUnderTest.RejectCallAsync(created.CallId, CalleeId, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.Rejected), Times.AtLeastOnce);
    }

    [Test]
    public async Task RejectCall_群组通话_仅通知呼叫方MemberRejected且通话继续()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, false, CancellationToken.None);

        var call = await _storeUnderTest.RejectCallAsync(created.CallId, CalleeId, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ringing));
        _client.Verify(x => x.MemberRejected(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CalleeId), Times.Once);
        _client.Verify(x => x.CallEnded(It.IsAny<CallInfoDto>(), It.IsAny<CallEndReason>()), Times.Never);
    }

    // ---------- 挂断 ----------

    [Test]
    public async Task HangUp_1对1通话_对方收到RemoteHangup结束()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);
        _client.Invocations.Clear();

        var call = await _storeUnderTest.HangUpAsync(created.CallId, CalleeId, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.RemoteHangup), Times.AtLeastOnce);
    }

    [Test]
    public async Task HangUp_群组最后一名成员离开_全员收AllLeft结束()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, false, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, Member3Id, null, CancellationToken.None);
        await _storeUnderTest.HangUpAsync(created.CallId, CalleeId, CancellationToken.None);
        await _storeUnderTest.HangUpAsync(created.CallId, CallerId, CancellationToken.None);
        _client.Invocations.Clear();

        var call = await _storeUnderTest.HangUpAsync(created.CallId, Member3Id, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.AllLeft), Times.AtLeastOnce);
    }

    // ---------- 取消 ----------

    [Test]
    public async Task CancelCall_呼叫方取消_广播CallerCancelled()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        var call = await _storeUnderTest.CancelCallAsync(created.CallId, CallerId, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.CallerCancelled), Times.AtLeastOnce);
    }

    [Test]
    public async Task CancelCall_非呼叫方取消_抛出异常()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.CancelCallAsync(created.CallId, CalleeId, CancellationToken.None));
    }

    // ---------- 信令 ----------

    [Test]
    public async Task ForwardSignal_未接通成员_抛出异常()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.ForwardSignalAsync(new CallSignalDto
            {
                CallId = created.CallId,
                FromUserId = CalleeId,
                Kind = "offer",
                Sdp = "v=0"
            }, CancellationToken.None));
    }

    [Test]
    public async Task ForwardSignal_接通后_定向转发给目标用户()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);
        _client.Invocations.Clear();

        await _storeUnderTest.ForwardSignalAsync(new CallSignalDto
        {
            CallId = created.CallId,
            FromUserId = CalleeId,
            ToUserId = CallerId,
            Kind = "answer",
            Sdp = "v=0 answer"
        }, CancellationToken.None);

        _client.Verify(x => x.Signal(It.Is<CallSignalDto>(s =>
            s.CallId == created.CallId && s.FromUserId == CalleeId && s.Kind == "answer")), Times.Once);
    }

    // ---------- 响铃超时 ----------

    [Test]
    public async Task GetCall_响铃超时_惰性终结并广播Timeout()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio, new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        // 将响铃超时置为 0（反射修改 static readonly 字段，测试后恢复）
        var field = typeof(CallSessionStore).GetField(
            "RingingTimeout", BindingFlags.Static | BindingFlags.NonPublic)!;
        var original = (TimeSpan)field.GetValue(null)!;
        try
        {
            field.SetValue(null, TimeSpan.Zero);

            var call = await _storeUnderTest.GetCallAsync(created.CallId, CancellationToken.None);

            Assert.That(call, Is.Not.Null);
            Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
            _client.Verify(x => x.CallEnded(
                It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.Timeout), Times.AtLeastOnce);
        }
        finally
        {
            field.SetValue(null, original);
        }
    }

    // ---------- 常驻房间 ----------

    [Test]
    public async Task CreateRoom_群组创建即Active_呼叫方Joined且索引含callId()
    {
        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, isRoom: true, CancellationToken.None);

        Assert.That(result.RoomKind, Is.EqualTo(CallRoomKind.Room));
        Assert.That(result.Status, Is.EqualTo(CallStatus.Active));
        Assert.That(result.JoinedMembers, Does.Contain(CallerId));
        Assert.That(_sets[$"message:call:session:{SessionId}"], Does.Contain(result.CallId.ToString()));
        // 创建者收到 CallStarted（多端同步）
        _client.Verify(x => x.CallStarted(
            It.Is<CallInfoDto>(c => c.CallId == result.CallId)), Times.AtLeastOnce);
    }

    [Test]
    public async Task CreateRoom_仅向被选在线成员推IncomingCall_未选者不推()
    {
        _onlineUsers.Add(CalleeId);
        _onlineUsers.Add(Member3Id);

        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio,
            new[] { CallerId, CalleeId, Member3Id },
            new[] { CalleeId }, null, isRoom: true, CancellationToken.None);

        // 仅被选者收到来电
        _client.Verify(x => x.IncomingCall(It.IsAny<CallInfoDto>()), Times.Once);
        Assert.That(result.InvitedMembers, Does.Contain(CalleeId));
        Assert.That(result.InvitedMembers, Does.Not.Contain(Member3Id));
    }

    [Test]
    public async Task CreateRoom_带密码_RequiresPassword为true且存哈希非明文()
    {
        var result = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, "secret123", isRoom: true, CancellationToken.None);

        Assert.That(result.RequiresPassword, Is.True);

        var storedJson = _store[$"message:call:{result.CallId}"];
        var stored = System.Text.Json.JsonSerializer.Deserialize<CallSession>(storedJson);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.RequiresPassword, Is.True);
        Assert.That(stored.PasswordHash, Is.Not.Null);
        Assert.That(stored.PasswordHash, Is.Not.EqualTo("secret123")); // 不存明文
        Assert.That(stored.PasswordHash!.Length, Is.EqualTo(64));      // SHA256 十六进制
    }

    [Test]
    public async Task AcceptRoom_密码错误_抛出异常()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, "secret123", isRoom: true, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, "wrong", CancellationToken.None));
        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None));
    }

    [Test]
    public async Task AcceptRoom_密码正确_加入并广播MemberJoined()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, "secret123", isRoom: true, CancellationToken.None);

        var call = await _storeUnderTest.AcceptCallAsync(
            created.CallId, CalleeId, "secret123", CancellationToken.None);

        Assert.That(call, Is.Not.Null);
        Assert.That(call!.JoinedMembers, Does.Contain(CalleeId));
        _client.Verify(x => x.MemberJoined(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CalleeId), Times.AtLeastOnce);
    }

    [Test]
    public async Task AcceptRoom_未设密码_未选成员可自由加入()
    {
        _onlineUsers.Add(Member3Id);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id },
            new[] { CalleeId }, null, isRoom: true, CancellationToken.None);

        // 未选成员 Member3Id 仍可加入（无密码房间）
        var call = await _storeUnderTest.AcceptCallAsync(
            created.CallId, Member3Id, null, CancellationToken.None);

        Assert.That(call, Is.Not.Null);
        Assert.That(call!.JoinedMembers, Does.Contain(Member3Id));
    }

    [Test]
    public async Task HangUp_房间全员离开_通话仍Active且索引保留_不触发AllLeft()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio,
            new[] { CallerId, CalleeId, Member3Id }, null, null, isRoom: true, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, CalleeId, null, CancellationToken.None);
        await _storeUnderTest.AcceptCallAsync(created.CallId, Member3Id, null, CancellationToken.None);
        _client.Invocations.Clear();

        var call = await _storeUnderTest.HangUpAsync(created.CallId, CallerId, CancellationToken.None);

        // 房间保持 Active，索引保留，不广播 AllLeft
        Assert.That(call!.Status, Is.EqualTo(CallStatus.Active));
        Assert.That(_sets[$"message:call:session:{SessionId}"], Does.Contain(created.CallId.ToString()));
        _client.Verify(x => x.CallEnded(It.IsAny<CallInfoDto>(), CallEndReason.AllLeft), Times.Never);
    }

    [Test]
    public async Task CloseRoom_仅创建者可关_非创建者抛异常()
    {
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, isRoom: true, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.CloseRoomAsync(created.CallId, CalleeId, CancellationToken.None));

        var call = await _storeUnderTest.CloseRoomAsync(created.CallId, CallerId, CancellationToken.None);

        Assert.That(call!.Status, Is.EqualTo(CallStatus.Ended));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.RoomClosed), Times.AtLeastOnce);
        Assert.That(_sets.ContainsKey($"message:call:session:{SessionId}") is false
            || _sets[$"message:call:session:{SessionId}"].Contains(created.CallId.ToString()), Is.False);
    }

    [Test]
    public async Task CloseRoom_1对1通话_抛出异常()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio,
            new[] { CallerId, CalleeId }, null, null, false, CancellationToken.None);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _storeUnderTest.CloseRoomAsync(created.CallId, CallerId, CancellationToken.None));
    }

    [Test]
    public async Task GetSessionRooms_返回活跃房间_排除已结束()
    {
        var room1 = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Video,
            new[] { CallerId, CalleeId, Member3Id }, null, null, isRoom: true, CancellationToken.None);
        var room2 = await _storeUnderTest.CreateCallAsync(
            SessionId, CalleeId, CallType.Audio,
            new[] { CallerId, CalleeId, Member3Id }, null, null, isRoom: true, CancellationToken.None);
        await _storeUnderTest.CloseRoomAsync(room1.CallId, CallerId, CancellationToken.None);

        var rooms = await _storeUnderTest.GetSessionRoomsAsync(SessionId, CancellationToken.None);

        Assert.That(rooms, Has.Count.EqualTo(1));
        Assert.That(rooms[0].CallId, Is.EqualTo(room2.CallId));
        Assert.That(rooms[0].Type, Is.EqualTo(CallType.Audio));
    }

    [Test]
    public async Task 二人房间_不按1对1处理_拒绝与挂断均不结束房间()
    {
        _onlineUsers.Add(CalleeId);
        var created = await _storeUnderTest.CreateCallAsync(
            SessionId, CallerId, CallType.Audio,
            new[] { CallerId, CalleeId }, null, null, isRoom: true, CancellationToken.None);

        // 2 人房间：房间已 Active，拒绝来电仅返回当前状态，不触发 1:1 的 Rejected 结束
        var rejected = await _storeUnderTest.RejectCallAsync(created.CallId, CalleeId, CancellationToken.None);
        Assert.That(rejected!.Status, Is.EqualTo(CallStatus.Active));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.Rejected), Times.Never);

        // 创建者挂断：房间仍 Active（非 RemoteHangup 结束）
        _client.Invocations.Clear();
        var call = await _storeUnderTest.HangUpAsync(created.CallId, CallerId, CancellationToken.None);
        Assert.That(call!.Status, Is.EqualTo(CallStatus.Active));
        _client.Verify(x => x.CallEnded(
            It.Is<CallInfoDto>(c => c.CallId == created.CallId), CallEndReason.RemoteHangup), Times.Never);
    }
}
