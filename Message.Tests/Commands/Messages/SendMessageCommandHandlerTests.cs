using Message.Domain.Entities.Chat;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Tests.TestHelpers;
using Message.Web.API.Application.Commands.Messages;
using Message.Web.API.Dto.Response;
using Message.Web.API.Grpc;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MessageEntity = Message.Domain.Entities.Chat.Message;

namespace Message.Tests.Commands.Messages;

/// <summary>
/// 发送消息命令处理程序单元测试。
/// 覆盖：8 种消息类型的分发（各自创建对应类型的消息实体并持久化）与不支持类型的异常。
/// </summary>
[TestFixture]
public class SendMessageCommandHandlerTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid SenderId = Guid.NewGuid();
    private static readonly Uri MediaUri = new("https://example.com/media.png");

    private static readonly Guid FileId = Guid.NewGuid();
    private static readonly Guid ThumbnailFileId = Guid.NewGuid();

    private Mock<IMessageRepository> _messageRepository = null!;
    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IFileAttachmentRepository> _fileRepository = null!;
    private Mock<IFileStorageGrpcClient> _fileStorage = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private SendMessageCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _messageRepository = new Mock<IMessageRepository>();
        _sessionRepository = new Mock<IChatSessionRepository>();
        _fileRepository = new Mock<IFileAttachmentRepository>();
        _fileStorage = new Mock<IFileStorageGrpcClient>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // FileDev 默认返回：文件存在且归属 SenderId
        _fileStorage.Setup(s => s.GetFileInfoAsync(FileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileInfoResult(true, FileId, SenderId, "file.png", 1024,
                new Uri("https://example.com/file.png"), "md5", "FileImage", null));
        _fileStorage.Setup(s => s.GetFileInfoAsync(ThumbnailFileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileInfoResult(true, ThumbnailFileId, SenderId, "thumb.png", 256,
                new Uri("https://example.com/thumb.png"), "md5", "FileImage", null));

        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId))
            .ReturnsAsync(new ChatSession(SessionType.Private, SenderId, new HashSet<Guid> { SenderId }));
        _sessionRepository.Setup(r => r.UpdateLastMessageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _messageRepository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>()))
            .ReturnsAsync((MessageEntity m) => m);
        _messageRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);

        // 媒体消息链路：附件记录与消息同事务落库
        _fileRepository.Setup(r => r.AddAsync(It.IsAny<FileAttachment>()))
            .ReturnsAsync((FileAttachment a) => a);

        _handler = new SendMessageCommandHandler(
            _messageRepository.Object,
            _sessionRepository.Object,
            _fileRepository.Object,
            _fileStorage.Object,
            new Mock<ILogger<SendMessageCommandHandler>>().Object,
            CacheServicesTestFactory.CreateUnreadCountCache(),
            CacheServicesTestFactory.CreateSessionCache(),
            CreateDeliveryService());
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


    /// <summary>构造一个携带全部可选参数的命令，测试时按需覆盖类型字段</summary>
    private static SendMessageCommand BuildCommand(MessageType type) => new(
        SessionId, SenderId, type,
        Content: "你好",
        FileId: FileId,
        ThumbnailFileId: ThumbnailFileId,
        Duration: 30,
        Caption: null,
        Latitude: 31.2,
        Longitude: 121.5,
        LocationName: "上海",
        LinkUrl: "https://example.com",
        LinkTitle: null,
        LinkDescription: null,
        ExpressionCode: "[smile]");

    [Test]
    public async Task Handler_文本消息_应创建文本消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageText), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageText)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_图片消息_应创建图片消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageImage), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageImage)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });

        // 重新设计 v2：媒体消息同时创建附件记录（FileDev 归属校验通过）
        _fileStorage.Verify(s => s.GetFileInfoAsync(FileId, It.IsAny<CancellationToken>()), Times.Once);
        _fileRepository.Verify(r => r.AddAsync(
            It.Is<FileAttachment>(a => a.FileId == FileId && a.MessageId == result)), Times.Once);
    }

    [Test]
    public async Task Handler_视频消息_应创建视频消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageVideo), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageVideo)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_音频消息_应创建音频消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageAudio), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageAudio)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_文件消息_应创建文件消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageFile), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageFile)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_位置消息_应创建位置消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageLocation), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageLocation)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_链接消息_应创建链接消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageLink), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageLink)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_表情消息_应创建表情消息并持久化()
    {
        var result = await _handler.Handler(BuildCommand(MessageType.MessageExpression), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.EqualTo(Guid.Empty));
            _messageRepository.Verify(r => r.AddAsync(
                It.Is<MessageEntity>(m => m.MessageType == MessageType.MessageExpression)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Handler_会话不存在时_应抛出InvalidOperationException()
    {
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync((ChatSession?)null);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handler(BuildCommand(MessageType.MessageText), CancellationToken.None));
    }

    [Test]
    public void Handler_不支持的消息类型_应抛出NotSupportedException()
    {
        Assert.ThrowsAsync<NotSupportedException>(async () =>
            await _handler.Handler(BuildCommand(MessageType.MessagePush), CancellationToken.None));
    }

    [Test]
    public async Task Handler_私聊会话_应设置消息接收者为对端用户()
    {
        var receiverId = Guid.NewGuid();
        var session = new ChatSession(SessionType.Private, SenderId,
            new HashSet<Guid> { SenderId, receiverId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);

        MessageEntity? saved = null;
        _messageRepository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>()))
            .Callback<MessageEntity>(m => saved = m)
            .ReturnsAsync((MessageEntity m) => m);

        await _handler.Handler(BuildCommand(MessageType.MessageText), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.ReceiverId, Is.EqualTo(receiverId));
        });
    }

    [Test]
    public async Task Handler_群聊会话_不应设置消息接收者()
    {
        var groupSession = new ChatSession(SessionType.Group, SenderId,
            new HashSet<Guid> { SenderId, Guid.NewGuid() });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(groupSession);

        MessageEntity? saved = null;
        _messageRepository.Setup(r => r.AddAsync(It.IsAny<MessageEntity>()))
            .Callback<MessageEntity>(m => saved = m)
            .ReturnsAsync((MessageEntity m) => m);

        await _handler.Handler(BuildCommand(MessageType.MessageText), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.ReceiverId, Is.Null);
        });
    }
}