using Message.Domain.Entities.Chat;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Tests.TestHelpers;
using Message.Web.API.Application.Commands.Messages;
using Message.Web.API.Grpc;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Tests.Commands.Messages;

/// <summary>
/// 消息 IDOR 负向测试（S-05）。
/// 覆盖：非参与者不得向会话发消息、非发送者不得撤回消息、转发需为源/目标会话成员。
/// </summary>
[TestFixture]
public class MessageIdorTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid TargetSessionId = Guid.NewGuid();
    private static readonly Guid SenderId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    private Mock<IMessageRepository> _messageRepository = null!;
    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _messageRepository = new Mock<IMessageRepository>();
        _sessionRepository = new Mock<IChatSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _messageRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
    }

    /// <summary>R-01：构造真实 MessageDeliveryService（连接管理器返回空连接，推送静默完成不干扰断言）。</summary>
    private static MessageDeliveryService CreateDeliveryService()
    {
        var connectionManager = new Mock<IConnectionManager>();
        connectionManager.Setup(c => c.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<string>());
        return new MessageDeliveryService(
            new Mock<IHubContext<MessageHub, IMessageClient>>().Object,
            connectionManager.Object,
            new Mock<ILogger<MessageDeliveryService>>().Object);
    }

    // ---------- SendMessageCommandHandler：非参与者禁止发送 ----------

    [Test]
    public async Task SendMessage_非参与者向会话发送消息_应抛出InvalidOperationException()
    {
        var session = new ChatSession(SessionType.Private, SenderId, new HashSet<Guid> { SenderId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);

        var handler = new SendMessageCommandHandler(
            _messageRepository.Object, _sessionRepository.Object,
            new Mock<IFileAttachmentRepository>().Object,
            new Mock<IFileStorageGrpcClient>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object,
            CacheServicesTestFactory.CreateUnreadCountCache(),
            CacheServicesTestFactory.CreateSessionCache(),
            CreateDeliveryService());

        // 模拟调用者（OutsiderId）试图以自己身份向他人会话发消息
        var command = new SendMessageCommand(
            SessionId, OutsiderId, MessageType.MessageText, "你好",
            FileId: null, ThumbnailFileId: null,
            Duration: null, Caption: null, Latitude: null, Longitude: null, LocationName: null,
            LinkUrl: null, LinkTitle: null, LinkDescription: null, ExpressionCode: null);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(command, CancellationToken.None));

        _messageRepository.Verify(r => r.AddAsync(It.IsAny<MessageEntity>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SendMessage_参与者向会话发送消息_应返回消息ID()
    {
        var session = new ChatSession(SessionType.Private, SenderId, new HashSet<Guid> { SenderId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _sessionRepository.Setup(r => r.UpdateLastMessageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _messageRepository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>()))
            .ReturnsAsync((MessageEntity m) => m);

        var handler = new SendMessageCommandHandler(
            _messageRepository.Object, _sessionRepository.Object,
            new Mock<IFileAttachmentRepository>().Object,
            new Mock<IFileStorageGrpcClient>().Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object,
            CacheServicesTestFactory.CreateUnreadCountCache(),
            CacheServicesTestFactory.CreateSessionCache(),
            CreateDeliveryService());

        var command = new SendMessageCommand(
            SessionId, SenderId, MessageType.MessageText, "你好",
            FileId: null, ThumbnailFileId: null,
            Duration: null, Caption: null, Latitude: null, Longitude: null, LocationName: null,
            LinkUrl: null, LinkTitle: null, LinkDescription: null, ExpressionCode: null);

        var result = await handler.Handler(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(It.IsAny<MessageEntity>()), Times.Once);
        });
    }

    // ---------- RecallMessageCommandHandler：仅发送者可撤回 ----------

    [Test]
    public async Task RecallMessage_非发送者撤回他人消息_应抛出InvalidOperationException()
    {
        var message = MessageEntity.CreateTextMessage(SessionId, SenderId, "你好");
        _messageRepository.Setup(r => r.GetByIdAsync(message.MessageId)).ReturnsAsync(message);

        var handler = new RecallMessageCommandHandler(
            _messageRepository.Object,
            new Mock<ILogger<RecallMessageCommandHandler>>().Object,
            CacheServicesTestFactory.CreateRedisCache(),
            CreateDeliveryService(),
            _sessionRepository.Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(
                new RecallMessageCommand(message.MessageId, OutsiderId, RecallReason.UserRequest),
                CancellationToken.None));

        Assert.That(message.IsRecalled, Is.False);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RecallMessage_发送者撤回自己的消息_应返回true()
    {
        var message = MessageEntity.CreateTextMessage(SessionId, SenderId, "你好");
        _messageRepository.Setup(r => r.GetByIdAsync(message.MessageId)).ReturnsAsync(message);
        _messageRepository.Setup(r => r.UpdateAsync(It.IsAny<MessageEntity>()))
            .ReturnsAsync((MessageEntity m) => m);

        var handler = new RecallMessageCommandHandler(
            _messageRepository.Object,
            new Mock<ILogger<RecallMessageCommandHandler>>().Object,
            CacheServicesTestFactory.CreateRedisCache(),
            CreateDeliveryService(),
            _sessionRepository.Object);

        var result = await handler.Handler(
            new RecallMessageCommand(message.MessageId, SenderId, RecallReason.UserRequest),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(message.IsRecalled, Is.True);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- ForwardMessageCommandHandler：源/目标会话成员校验 ----------

    [Test]
    public async Task ForwardMessage_非源会话成员转发_应抛出UnauthorizedAccessException()
    {
        var original = MessageEntity.CreateTextMessage(SessionId, SenderId, "你好");
        _messageRepository.Setup(r => r.GetByIdAsync(original.MessageId)).ReturnsAsync(original);

        // 源会话不含 OutsiderId
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId, new HashSet<Guid> { SenderId }));

        var handler = new ForwardMessageCommandHandler(
            _messageRepository.Object, _sessionRepository.Object,
            new Mock<ILogger<ForwardMessageCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(
                new ForwardMessageCommand(original.MessageId, TargetSessionId, OutsiderId,
                    ForwardType.Direct, null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ForwardMessage_非目标会话成员转发_应抛出UnauthorizedAccessException()
    {
        var original = MessageEntity.CreateTextMessage(SessionId, SenderId, "你好");
        _messageRepository.Setup(r => r.GetByIdAsync(original.MessageId)).ReturnsAsync(original);

        // 源会话含 OutsiderId，目标会话不含
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId,
                new HashSet<Guid> { SenderId, OutsiderId }));
        _sessionRepository.Setup(r => r.GetByIdAsync(TargetSessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId, new HashSet<Guid> { SenderId }));

        var handler = new ForwardMessageCommandHandler(
            _messageRepository.Object, _sessionRepository.Object,
            new Mock<ILogger<ForwardMessageCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(
                new ForwardMessageCommand(original.MessageId, TargetSessionId, OutsiderId,
                    ForwardType.Direct, null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ForwardMessage_源目标会话成员转发_应返回新消息ID()
    {
        var original = MessageEntity.CreateTextMessage(SessionId, SenderId, "你好");
        _messageRepository.Setup(r => r.GetByIdAsync(original.MessageId)).ReturnsAsync(original);
        _messageRepository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>()))
            .ReturnsAsync((MessageEntity m) => m);

        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId,
                new HashSet<Guid> { SenderId, OutsiderId }));
        _sessionRepository.Setup(r => r.GetByIdAsync(TargetSessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, OutsiderId,
                new HashSet<Guid> { SenderId, OutsiderId }));

        var handler = new ForwardMessageCommandHandler(
            _messageRepository.Object, _sessionRepository.Object,
            new Mock<ILogger<ForwardMessageCommandHandler>>().Object);

        var result = await handler.Handler(
            new ForwardMessageCommand(original.MessageId, TargetSessionId, OutsiderId,
                ForwardType.Direct, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result, Is.Not.EqualTo(original.MessageId));
            _messageRepository.Verify(r => r.AddAsync(It.IsAny<MessageEntity>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}