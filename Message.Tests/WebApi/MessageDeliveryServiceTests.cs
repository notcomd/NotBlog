using Message.Domain.Enums;
using Message.Domain.IServices;
using Message.Web.API.Dto;
using Message.Web.API.Dto.Response;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.WebApi;

/// <summary>
/// 消息实时推送服务（<see cref="MessageDeliveryService"/>）单元测试。
/// 核心验证：按连接并行推送、排除指定连接、无在线连接短路、会话群组命名、
/// 输入指示/已读回执/撤回/未读/上下线等实时事件的正确投递。
/// </summary>
[TestFixture]
public class MessageDeliveryServiceTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();

    private Mock<IHubContext<MessageHub, IMessageClient>> _hubContext = null!;
    private Mock<IHubClients<IMessageClient>> _clients = null!;
    private Mock<IMessageClient> _clientProxy = null!;
    private Mock<IConnectionManager> _connectionManager = null!;
    private MessageDeliveryService _service = null!;

    [SetUp]
    public void Setup()
    {
        _clientProxy = new Mock<IMessageClient>();
        // 所有客户端回调均返回 CompletedTask，避免 Task.WhenAll 收到 null
        _clientProxy.Setup(c => c.ReceiveMessage(It.IsAny<MessageDto>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.MessageRecalled(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.MessageRead(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.TypingIndicator(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.UnreadCountUpdated(It.IsAny<Guid>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.UploadProgress(It.IsAny<ChunkUploadProgress>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.UserOnline(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _clientProxy.Setup(c => c.UserOffline(It.IsAny<Guid>())).Returns(Task.CompletedTask);

        _clients = new Mock<IHubClients<IMessageClient>>();
        _clients.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxy.Object);
        _clients.Setup(c => c.User(It.IsAny<string>())).Returns(_clientProxy.Object);

        _hubContext = new Mock<IHubContext<MessageHub, IMessageClient>>();
        _hubContext.Setup(h => h.Clients).Returns(_clients.Object);

        _connectionManager = new Mock<IConnectionManager>();

        _service = new MessageDeliveryService(
            _hubContext.Object,
            _connectionManager.Object,
            new Mock<ILogger<MessageDeliveryService>>().Object);
    }

    private static MessageDto BuildMessage() => new()
    {
        MessageId = Guid.NewGuid(),
        SessionId = SessionId,
        SenderId = UserA,
        MessageType = MessageType.MessageText,
        Content = "你好"
    };

    [Test]
    public void SessionGroupName_应采用session前缀格式()
    {
        var groupName = MessageDeliveryService.SessionGroupName(SessionId);

        Assert.That(groupName, Is.EqualTo($"session:{SessionId}"));
    }

    [Test]
    public void DeliverMessageAsync_应并行推送给所有参与者连接()
    {
        var message = BuildMessage();
        var participants = new[] { UserA, UserB };
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

        _service.DeliverMessageAsync(SessionId, message, participants).Wait();

        _clientProxy.Verify(c => c.ReceiveMessage(It.Is<MessageDto>(m => m.MessageId == message.MessageId)),
            Times.Exactly(2), "两个参与者的连接都应收到消息");
    }

    [Test]
    public void DeliverMessageAsync_应去重并排除指定连接()
    {
        var message = BuildMessage();
        var participants = new[] { UserA, UserB };
        _connectionManager.Setup(m => m.GetConnectionsAsync(UserA))
            .ReturnsAsync(new[] { "conn-A", "conn-dup" });
        _connectionManager.Setup(m => m.GetConnectionsAsync(UserB))
            .ReturnsAsync(new[] { "conn-B", "conn-dup" });

        // 排除发送者当前连接（不回显）
        _service.DeliverMessageAsync(SessionId, message, participants, excludeConnectionId: "conn-A").Wait();

        _clientProxy.Verify(c => c.ReceiveMessage(It.IsAny<MessageDto>()),
            Times.Exactly(2), "去重后 3 个连接（conn-A/conn-B/conn-dup），排除 conn-A 后应推送 2 次");
    }

    [Test]
    public void DeliverMessageAsync_无在线连接时应直接返回()
    {
        var message = BuildMessage();
        var participants = new[] { UserA, UserB };
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<string>());

        _service.DeliverMessageAsync(SessionId, message, participants).Wait();

        _clientProxy.Verify(c => c.ReceiveMessage(It.IsAny<MessageDto>()), Times.Never);
    }

    [Test]
    public void GetOnlineConnectionsAsync_应并行查询并去重()
    {
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(UserA))
            .ReturnsAsync(new[] { "c1" });
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(UserB))
            .ReturnsAsync(new[] { "c2", "c2" });

        var connections = _service.GetOnlineConnectionsAsync(new[] { UserA, UserB }).Result;

        Assert.That(connections, Is.EquivalentTo(new[] { "c1", "c2" }));
        _connectionManager.Verify(m => m.GetConnectionsAsync(UserA), Times.Once);
        _connectionManager.Verify(m => m.GetConnectionsAsync(UserB), Times.Once);
    }

    [Test]
    public void NotifyTypingAsync_应排除输入者自身()
    {
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

        _service.NotifyTypingAsync(SessionId, UserA, new[] { UserA, UserB }).Wait();

        // 仅 UserB 的连接收到 TypingIndicator
        _clientProxy.Verify(c => c.TypingIndicator(SessionId, UserA), Times.Once);
        _connectionManager.Verify(m => m.GetConnectionsAsync(UserA), Times.Never,
            "输入者自身不应被通知");
        _connectionManager.Verify(m => m.GetConnectionsAsync(UserB), Times.Once);
    }

    [Test]
    public void NotifyMessageReadAsync_应通知除阅读者外的参与者()
    {
        var messageId = Guid.NewGuid();
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

        _service.NotifyMessageReadAsync(SessionId, messageId, UserA, new[] { UserA, UserB }).Wait();

        _clientProxy.Verify(c => c.MessageRead(messageId, UserA), Times.Once);
        _connectionManager.Verify(m => m.GetConnectionsAsync(UserA), Times.Never);
    }

    [Test]
    public void NotifyMessageRecalledAsync_应通知全部参与者()
    {
        var messageId = Guid.NewGuid();
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

        _service.NotifyMessageRecalledAsync(SessionId, messageId, new[] { UserA, UserB }).Wait();

        _clientProxy.Verify(c => c.MessageRecalled(messageId), Times.Exactly(2));
    }

    [Test]
    public void NotifyUnreadCountAsync_应通过User通道推送未读数()
    {
        _service.NotifyUnreadCountAsync(SessionId, UserB, 5).Wait();

        _clientProxy.Verify(c => c.UnreadCountUpdated(SessionId, 5), Times.Once);
    }

    [Test]
    public void PushUploadProgressAsync_应推送分片上传进度()
    {
        var progress = new ChunkUploadProgress("file-key", 3, 5, 60, 2);

        _service.PushUploadProgressAsync(UserA, progress).Wait();

        _clientProxy.Verify(c => c.UploadProgress(
            It.Is<ChunkUploadProgress>(p => p.FileKey == "file-key" && p.Percent == 60)),
            Times.Once);
    }

    [Test]
    public void NotifyUserStatusChangedAsync_应推送上下线事件()
    {
        _connectionManager
            .Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new[] { $"conn-{id}" });

        _service.NotifyUserStatusChangedAsync(UserB, true, new[] { UserA }).Wait();
        _clientProxy.Verify(c => c.UserOnline(UserB), Times.Once);

        _service.NotifyUserStatusChangedAsync(UserB, false, new[] { UserA }).Wait();
        _clientProxy.Verify(c => c.UserOffline(UserB), Times.Once);
    }
}
