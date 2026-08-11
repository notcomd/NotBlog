using Message.Domain.Entities.Chat;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Domain.IServices;
using Message.Infrastructure.Services;
using Message.Web.API.Application.Commands.Messages;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Request;
using Message.Web.API.Dto.Response;
using Message.Web.API.Grpc;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NotMediator;
using Message.Tests.TestHelpers;
using StackExchange.Redis;
using System.Security.Claims;
using DomainMessage = Message.Domain.Entities.Chat.Message;

namespace Message.Tests.WebApi;

/// <summary>
/// 实时通信 Hub（<see cref="MessageHub"/>）单元测试。
/// 覆盖：连接生命周期（在线登记/离线消息补推/双通道认证）、
/// 会话消息收发（校验越权、参与者并行推送、会话群组广播）、
/// 群组管理（加入/离开）、已读/撤回通知，以及分片上传/断点续传的 gRPC 委托。
/// </summary>
[TestFixture]
public class MessageHubTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();
    private const string FileKey = "chunk-key-001";

    private HubHarness _harness = null!;

    [SetUp]
    public void Setup()
    {
        _harness = new HubHarness(UserId);
    }

    // ─────────────────────────── 连接生命周期 ───────────────────────────

    [Test]
    public async Task OnConnectedAsync_应登记连接并置为在线()
    {
        await _harness.Hub.OnConnectedAsync();

        _harness.ConnectionCommandService.Verify(m => m.AddConnectionAsync(UserId, "conn-test"), Times.Once);
        // Q-05：在线状态经 UserStatusCacheService 写入 Redis（message:online:users 集合）
        _harness.UserStatusDb.Verify(d => d.SetAddAsync("message:online:users", UserId.ToString(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_有离线消息时应补推()
    {
        var unread = DomainMessage.CreateTextMessage(SessionId, OtherUserId, "离线消息");
        _harness.MessageRepository
            .Setup(m => m.GetUnreadMessagesAsync(UserId))
            .ReturnsAsync(new[] { unread });

        await _harness.Hub.OnConnectedAsync();

        _harness.CallerProxy.Verify(c => c.ReceiveMessage(
            It.Is<MessageDto>(m => m.MessageId == unread.MessageId)), Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_无JWT时应回退到当前用户服务()
    {
        _harness = new HubHarness(UserId, hasJwtClaim: false);
        _harness.CurrentUser.Setup(m => m.GetUserId()).Returns(UserId);

        await _harness.Hub.OnConnectedAsync();

        _harness.ConnectionCommandService.Verify(m => m.AddConnectionAsync(UserId, "conn-test"), Times.Once);
    }

    [Test]
    public async Task OnDisconnectedAsync_仅最后连接断开时才置为离线()
    {
        _harness.ConnectionManager.Setup(m => m.HasOtherConnectionsAsync(UserId)).ReturnsAsync(false);

        await _harness.Hub.OnDisconnectedAsync(null);

        _harness.ConnectionCommandService.Verify(m => m.RemoveConnectionAsync(UserId, "conn-test"), Times.Once);
        // Q-05：离线状态经 UserStatusCacheService 写入 Redis（从 message:online:users 集合移除）
        _harness.UserStatusDb.Verify(d => d.SetRemoveAsync("message:online:users", UserId.ToString(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    // ─────────────────────────── 会话消息 ───────────────────────────

    [Test]
    public async Task SendMessage_请求为空应抛出HubException()
    {
        Assert.That(() => _harness.Hub.SendMessage(SessionId, null!),
            Throws.TypeOf<HubException>().With.Message.Contains("消息请求不能为空"));
    }

    [Test]
    public async Task SendMessage_会话不存在应抛出HubException()
    {
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId))
            .ReturnsAsync((ChatSession?)null);

        Assert.That(() => _harness.Hub.SendMessage(SessionId, new SendMessageRequest { Content = "hi" }),
            Throws.TypeOf<HubException>().With.Message.Contains("会话不存在"));
    }

    [Test]
    public async Task SendMessage_非参与者应抛出HubException()
    {
        var session = ChatSession.CreatePrivateSession(OtherUserId, Guid.NewGuid());
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId)).ReturnsAsync(session);

        Assert.That(() => _harness.Hub.SendMessage(SessionId, new SendMessageRequest { Content = "hi" }),
            Throws.TypeOf<HubException>().With.Message.Contains("不是该会话的参与者"));
    }

    [Test]
    public async Task SendMessage_成功应并行推送到参与者并广播到会话群组()
    {
        var session = ChatSession.CreatePrivateSession(UserId, OtherUserId);
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId)).ReturnsAsync(session);

        // 重新设计 v2：消息经 SendMessageCommand 命令链路创建，Hub 凭返回的 MessageId 重新加载实体
        var message = DomainMessage.CreateTextMessage(SessionId, UserId, "你好");
        _harness.Mediator
            .Setup(m => m.SendAsync(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message.MessageId);
        _harness.MessageRepository
            .Setup(m => m.GetByIdAsync(message.MessageId))
            .ReturnsAsync(message);

        // 先加入会话群组（模拟客户端订阅）
        await _harness.Hub.JoinSession(SessionId);

        await _harness.Hub.SendMessage(SessionId, new SendMessageRequest
        {
            MessageType = MessageType.MessageText,
            Content = "你好"
        });

        // 命令链路被调用（消息经 SendMessageCommand 创建，Hub 不再自行实现）
        _harness.Mediator.Verify(m => m.SendAsync(
            It.Is<SendMessageCommand>(c => c.MessageType == MessageType.MessageText),
            It.IsAny<CancellationToken>()), Times.Once);
        // 1) 按连接并行推送（两个参与者各自 1 个连接）
        _harness.DeliveryProxy.Verify(c => c.ReceiveMessage(
            It.Is<MessageDto>(m => m.MessageId == message.MessageId)), Times.Exactly(2));
        // 2) 会话群组广播
        _harness.GroupProxy.Verify(c => c.ReceiveMessage(
            It.Is<MessageDto>(m => m.MessageId == message.MessageId)), Times.Once);
    }

    [Test]
    public async Task SendMessage_未加入群组时不应广播到群组()
    {
        var session = ChatSession.CreatePrivateSession(UserId, OtherUserId);
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId)).ReturnsAsync(session);

        var message = DomainMessage.CreateTextMessage(SessionId, UserId, "你好");
        _harness.Mediator
            .Setup(m => m.SendAsync(It.IsAny<SendMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message.MessageId);
        _harness.MessageRepository
            .Setup(m => m.GetByIdAsync(message.MessageId))
            .ReturnsAsync(message);

        await _harness.Hub.SendMessage(SessionId, new SendMessageRequest
        {
            MessageType = MessageType.MessageText,
            Content = "你好"
        });

        _harness.GroupProxy.Verify(c => c.ReceiveMessage(It.IsAny<MessageDto>()), Times.Never);
    }

    [Test]
    public async Task MarkAsRead_应通知会话内其他参与者()
    {
        var message = DomainMessage.CreateTextMessage(SessionId, OtherUserId, "hi");
        _harness.MessageRepository.Setup(m => m.GetByIdAsync(message.MessageId)).ReturnsAsync(message);
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId))
            .ReturnsAsync(ChatSession.CreatePrivateSession(UserId, OtherUserId));

        await _harness.Hub.MarkAsRead(message.MessageId);

        _harness.MessageRepository.Verify(m => m.MarkAsReadAsync(message.MessageId, UserId), Times.Once);
        _harness.CallerProxy.Verify(c => c.MessageRead(message.MessageId, UserId), Times.Once);
        // 其他参与者（OtherUserId）的连接收到已读通知
        _harness.ConnectionManager.Verify(m => m.GetConnectionsAsync(OtherUserId), Times.Once);
        _harness.DeliveryProxy.Verify(c => c.MessageRead(message.MessageId, UserId), Times.Once);
    }

    [Test]
    public async Task RecallMessage_应通知会话内所有参与者()
    {
        // 修复 S-05：仅消息发送者可撤回，故消息必须由当前用户（UserId）发送
        var message = DomainMessage.CreateTextMessage(SessionId, UserId, "hi");
        _harness.MessageRepository.Setup(m => m.GetByIdAsync(message.MessageId)).ReturnsAsync(message);
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId))
            .ReturnsAsync(ChatSession.CreatePrivateSession(UserId, OtherUserId));

        await _harness.Hub.RecallMessage(message.MessageId);

        _harness.MessageRepository.Verify(m => m.UpdateAsync(message), Times.Once);
        _harness.CallerProxy.Verify(c => c.MessageRecalled(message.MessageId), Times.Once);
        // 所有参与者（含自己）的连接收到撤回通知
        _harness.DeliveryProxy.Verify(c => c.MessageRecalled(message.MessageId), Times.Exactly(2));
    }

    // ─────────────────────────── 会话群组 ───────────────────────────

    [Test]
    public async Task JoinSession_应将连接加入会话群组()
    {
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId))
            .ReturnsAsync(ChatSession.CreatePrivateSession(UserId, OtherUserId));

        await _harness.Hub.JoinSession(SessionId);

        _harness.Groups.Verify(g => g.AddToGroupAsync("conn-test", $"session:{SessionId}",
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(_harness.Items.ContainsKey($"joined:{SessionId}"), Is.True,
            "加入成功后应在连接上下文中标记群组订阅状态");
    }

    [Test]
    public async Task JoinSession_非参与者应抛出HubException()
    {
        var session = ChatSession.CreatePrivateSession(Guid.NewGuid(), Guid.NewGuid());
        _harness.SessionRepository.Setup(m => m.GetByIdAsync(SessionId)).ReturnsAsync(session);

        Assert.That(() => _harness.Hub.JoinSession(SessionId),
            Throws.TypeOf<HubException>().With.Message.Contains("不是该会话的参与者"));
    }

    [Test]
    public async Task LeaveSession_应将连接移出会话群组()
    {
        await _harness.Hub.LeaveSession(SessionId);

        _harness.Groups.Verify(g => g.RemoveFromGroupAsync("conn-test", $"session:{SessionId}",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────── 分片上传（gRPC 委托） ───────────────────────────

    [Test]
    public async Task UploadChunk_应委托gRPC客户端上传()
    {
        var chunkData = new byte[] { 1, 2, 3 };
        _harness.FileStorageGrpc
            .Setup(m => m.UploadChunkAsync(FileKey, 2, chunkData, "chunk-md5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChunkUploadResult(true, 2, "chunk-md5", null));

        var result = await _harness.Hub.UploadChunk(new ChunkUploadRequest
        {
            FileKey = FileKey,
            ChunkIndex = 2,
            ChunkData = chunkData,
            ChunkMd5 = "chunk-md5"
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            _harness.FileStorageGrpc.Verify(m => m.UploadChunkAsync(
                FileKey, 2, chunkData, "chunk-md5", It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task InitChunkUpload_应委托gRPC客户端初始化()
    {
        _harness.FileStorageGrpc
            .Setup(m => m.InitChunkUploadAsync(UserId, "a.mp4", 100, "md5", "描述", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChunkUploadInitResult(true, FileKey, 4, 1024, [0], null));

        var result = await _harness.Hub.InitChunkUpload(new ChunkUploadInitRequest
        {
            FileName = "a.mp4",
            TotalSize = 100,
            FileMd5 = "md5",
            Description = "描述",
            IsPublic = true
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.FileKey, Is.EqualTo(FileKey));
            _harness.FileStorageGrpc.Verify(m => m.InitChunkUploadAsync(
                UserId, "a.mp4", 100, "md5", "描述", true, It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task GetChunkStatus_应委托gRPC客户端查询()
    {
        _harness.FileStorageGrpc
            .Setup(m => m.GetChunkStatusAsync(FileKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChunkStatusResult(true, FileKey, 4, [0, 2], "Uploading", null));

        var result = await _harness.Hub.GetChunkStatus(new ChunkStatusRequest { FileKey = FileKey });

        Assert.That(result.UploadedChunks, Is.EquivalentTo(new[] { 0, 2 }));
    }

    [Test]
    public async Task ResumeChunkUpload_应委托gRPC客户端并把进度推送给调用者()
    {
        var chunks = new Dictionary<int, byte[]> { [0] = new byte[] { 1 } };
        _harness.FileStorageGrpc
            .Setup(m => m.ResumeChunkUploadAsync(FileKey, 4, 1024, It.IsAny<IReadOnlyDictionary<int, byte[]>>(),
                It.IsAny<IProgress<ChunkUploadProgress>>(), It.IsAny<CancellationToken>()))
            .Callback((string _, int total, int _, IReadOnlyDictionary<int, byte[]> _,
                IProgress<ChunkUploadProgress>? progress, CancellationToken _) =>
            {
                // 模拟 gRPC 客户端上传过程中回调进度
                progress?.Report(new ChunkUploadProgress(FileKey, 1, total, 25, 0));
            })
            .ReturnsAsync(new ChunkStatusResult(true, FileKey, 4, [0], "Uploading", null));

        var result = await _harness.Hub.ResumeChunkUpload(new ChunkResumeRequest
        {
            FileKey = FileKey,
            TotalChunks = 4,
            ChunkSize = 1024,
            Chunks = chunks
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            _harness.FileStorageGrpc.Verify(m => m.ResumeChunkUploadAsync(FileKey, 4, 1024, chunks,
                It.IsAny<IProgress<ChunkUploadProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
            // 进度通过 SignalR UploadProgress 实时推送给调用者连接
            _harness.CallerProxy.Verify(c => c.UploadProgress(
                It.Is<ChunkUploadProgress>(p => p.FileKey == FileKey && p.Percent == 25)), Times.Once);
        });
    }

    [Test]
    public async Task ResumeChunkUpload_请求为空应抛出HubException()
    {
        Assert.That(() => _harness.Hub.ResumeChunkUpload(null!),
            Throws.TypeOf<HubException>().With.Message.Contains("断点续传请求不能为空"));
    }

    // ═══════════════════════════════════════════════════════
    // 测试夹具：集中构建 MessageHub 及其全部依赖 Mock
    // ═══════════════════════════════════════════════════════

    private sealed class HubHarness
    {
        public const string ConnectionId = "conn-test";

        public Mock<IMessageRepository> MessageRepository { get; }
        public Mock<IChatSessionRepository> SessionRepository { get; }
        public Mock<IUnitOfWork> UnitOfWork { get; }
        public Mock<IConnectionManager> ConnectionManager { get; }
        public Mock<IConnectionCommandService> ConnectionCommandService { get; }
        public Mock<IFileStorageGrpcClient> FileStorageGrpc { get; }
        public Mock<INotMediator> Mediator { get; }
        public Mock<ICurrentUserService> CurrentUser { get; }
        public Mock<IGroupManager> Groups { get; }
        public Mock<IMessageClient> DeliveryProxy { get; }
        public Mock<IMessageClient> GroupProxy { get; }
        public Mock<IMessageClient> CallerProxy { get; }
        public Mock<IDatabase> UserStatusDb { get; }
        public Mock<IMessageFriendsRepository> FriendsRepository { get; }
        public Mock<HubCallerContext> Context { get; }
        public Dictionary<object, object?> Items { get; } = new();
        public MessageHub Hub { get; }

        public HubHarness(Guid userId, bool hasJwtClaim = true)
        {
            DeliveryProxy = CreateProxy();
            GroupProxy = CreateProxy();
            CallerProxy = CreateProxy();
            UserStatusDb = new Mock<IDatabase>();
            Mediator = new Mock<INotMediator>();
            FriendsRepository = new Mock<IMessageFriendsRepository>();
            // R-09：默认无好友（在线状态推送静默完成）
            FriendsRepository.Setup(r => r.GetFriendIdsAsync(It.IsAny<Guid>()))
                .ReturnsAsync(Array.Empty<Guid>());

            var clients = new Mock<IHubCallerClients<IMessageClient>>();
            clients.Setup(c => c.Client(It.IsAny<string>())).Returns(DeliveryProxy.Object);
            clients.Setup(c => c.Group(It.IsAny<string>())).Returns(GroupProxy.Object);
            clients.SetupGet(c => c.Caller).Returns(CallerProxy.Object);

            var hubContext = new Mock<IHubContext<MessageHub, IMessageClient>>();
            hubContext.Setup(h => h.Clients).Returns(clients.Object);

            ConnectionManager = new Mock<IConnectionManager>();
            ConnectionManager.Setup(m => m.HasOtherConnectionsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            ConnectionManager.Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

            // CQRS：命令侧（连接登记/注销）经 IConnectionCommandService 分发
            ConnectionCommandService = new Mock<IConnectionCommandService>();
            ConnectionCommandService.Setup(m => m.AddConnectionAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            ConnectionCommandService.Setup(m => m.RemoveConnectionAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            UnitOfWork = new Mock<IUnitOfWork>();
            UnitOfWork.Setup(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            MessageRepository = new Mock<IMessageRepository>();
            MessageRepository.Setup(m => m.GetUnreadMessagesAsync(It.IsAny<Guid>()))
                .ReturnsAsync(Array.Empty<DomainMessage>());
            MessageRepository.Setup(m => m.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            MessageRepository.Setup(m => m.AddAsync(It.IsAny<DomainMessage>()))
                .ReturnsAsync((DomainMessage m) => m);
            MessageRepository.Setup(m => m.UpdateAsync(It.IsAny<DomainMessage>()))
                .ReturnsAsync((DomainMessage m) => m);
            MessageRepository.SetupGet(m => m.UnitOfWork).Returns(UnitOfWork.Object);

            SessionRepository = new Mock<IChatSessionRepository>();
            SessionRepository.Setup(m => m.UpdateLastMessageAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>()))
                .Returns(Task.CompletedTask);
            SessionRepository.Setup(m => m.UpdateAsync(It.IsAny<ChatSession>()))
                .ReturnsAsync((ChatSession s) => s);
            SessionRepository.SetupGet(m => m.UnitOfWork).Returns(UnitOfWork.Object);

            FileStorageGrpc = new Mock<IFileStorageGrpcClient>();
            CurrentUser = new Mock<ICurrentUserService>();

            Groups = new Mock<IGroupManager>();
            Groups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Groups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Context = new Mock<HubCallerContext>();
            Context.SetupGet(c => c.ConnectionId).Returns(ConnectionId);
            Context.SetupGet(c => c.ConnectionAborted).Returns(CancellationToken.None);
            Context.SetupGet(c => c.Items).Returns(Items);
            Context.SetupGet(c => c.User).Returns(hasJwtClaim
                ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }))
                : new ClaimsPrincipal(new ClaimsIdentity()));

            var deliveryService = new MessageDeliveryService(
                hubContext.Object,
                ConnectionManager.Object,
                new Mock<ILogger<MessageDeliveryService>>().Object);

            Hub = new MessageHub(
                MessageRepository.Object,
                SessionRepository.Object,
                UnitOfWork.Object,
                ConnectionManager.Object,
                ConnectionCommandService.Object,
                deliveryService,
                FileStorageGrpc.Object,
                CurrentUser.Object,
                new Mock<ILogger<MessageHub>>().Object,
                CacheServicesTestFactory.CreateUserStatusCache(UserStatusDb),
                CacheServicesTestFactory.CreateUnreadCountCache(),
                CacheServicesTestFactory.CreateSessionCache(),
                CacheServicesTestFactory.CreateRedisCache(),
                Mediator.Object,
                FriendsRepository.Object)
            {
                Context = Context.Object,
                Clients = clients.Object,
                Groups = Groups.Object
            };
        }

        private static Mock<IMessageClient> CreateProxy()
        {
            var proxy = new Mock<IMessageClient>();
            proxy.Setup(c => c.ReceiveMessage(It.IsAny<MessageDto>())).Returns(Task.CompletedTask);
            proxy.Setup(c => c.MessageRead(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
            proxy.Setup(c => c.MessageRecalled(It.IsAny<Guid>())).Returns(Task.CompletedTask);
            proxy.Setup(c => c.UploadProgress(It.IsAny<ChunkUploadProgress>())).Returns(Task.CompletedTask);
            proxy.Setup(c => c.TypingIndicator(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
            return proxy;
        }
    }
}