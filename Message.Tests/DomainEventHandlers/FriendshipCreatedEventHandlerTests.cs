using Commons.SeedWork;
using Message.Domain.Entities.Tweet;
using Message.Domain.Entities.User;
using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Web.API.Application.DomainEventHandlers;
using Message.Web.API.Dto.Response;
using Message.Web.API.Hubs;
using Message.Web.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.DomainEventHandlers;

/// <summary>
/// 好友请求领域事件处理程序单元测试。
/// 覆盖：为接收者生成「好友请求」站内通知并实时推送、昵称缺失回退、推送失败不阻断通知落库。
/// </summary>
[TestFixture]
public class FriendshipCreatedEventHandlerTests
{
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid ReceiverId = Guid.NewGuid();

    private Mock<ITweetNotificationRepository> _notificationRepository = null!;
    private Mock<IUserInfoRepository> _userInfoRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IConnectionManager> _connectionManager = null!;
    private Mock<IMessageClient> _clientProxy = null!;
    private MessageDeliveryService _deliveryService = null!;

    [SetUp]
    public void Setup()
    {
        _notificationRepository = new Mock<ITweetNotificationRepository>();
        _userInfoRepository = new Mock<IUserInfoRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _connectionManager = new Mock<IConnectionManager>();

        _clientProxy = new Mock<IMessageClient>();
        _clientProxy.Setup(c => c.PushNotification(It.IsAny<NotificationDto>())).Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients<IMessageClient>>();
        clients.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxy.Object);

        var hubContext = new Mock<IHubContext<MessageHub, IMessageClient>>();
        hubContext.Setup(h => h.Clients).Returns(clients.Object);

        _deliveryService = new MessageDeliveryService(
            hubContext.Object,
            _connectionManager.Object,
            new Mock<ILogger<MessageDeliveryService>>().Object);
    }

    private FriendshipCreatedEventHandler CreateHandler() => new(
        _notificationRepository.Object,
        _userInfoRepository.Object,
        _unitOfWork.Object,
        _deliveryService,
        new Mock<ILogger<FriendshipCreatedEventHandler>>().Object);

    [Test]
    public async Task Handler_应给接收者生成好友请求通知并实时推送()
    {
        _userInfoRepository.Setup(r => r.GetByUserIdAsync(RequesterId))
            .ReturnsAsync(UserInfo.Create(RequesterId, "requester@test.com", "小明"));
        _connectionManager.Setup(m => m.GetConnectionsAsync(ReceiverId))
            .ReturnsAsync(new[] { "conn-receiver" });

        TweetNotification? saved = null;
        _notificationRepository.Setup(r => r.AddAsync(It.IsAny<TweetNotification>()))
            .Callback<TweetNotification>(n => saved = n)
            .ReturnsAsync((TweetNotification n) => n);

        await CreateHandler().Handler(new FriendshipCreatedEvent(RequesterId, ReceiverId));

        Assert.Multiple(() =>
        {
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.UserGuid, Is.EqualTo(ReceiverId), "通知接收者应为被请求方");
            Assert.That(saved.Type, Is.EqualTo(NotificationType.FriendRequestReceived));
            Assert.That(saved.Title, Is.EqualTo("好友请求"));
            Assert.That(saved.Content, Is.EqualTo("小明 请求添加您为好友"));
            Assert.That(saved.RefType, Is.EqualTo("User"));
            Assert.That(saved.RefGuid, Is.EqualTo(RequesterId), "关联目标应为请求方，便于前端直接处理");
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _clientProxy.Verify(c => c.PushNotification(
                It.Is<NotificationDto>(d => d.Type == nameof(NotificationType.FriendRequestReceived))), Times.Once);
        });
    }

    [Test]
    public async Task Handler_请求者昵称缺失时_应回退通用称谓()
    {
        _userInfoRepository.Setup(r => r.GetByUserIdAsync(RequesterId)).ReturnsAsync((UserInfo?)null);

        TweetNotification? saved = null;
        _notificationRepository.Setup(r => r.AddAsync(It.IsAny<TweetNotification>()))
            .Callback<TweetNotification>(n => saved = n)
            .ReturnsAsync((TweetNotification n) => n);

        await CreateHandler().Handler(new FriendshipCreatedEvent(RequesterId, ReceiverId));

        Assert.That(saved!.Content, Is.EqualTo("一位用户 请求添加您为好友"));
    }

    [Test]
    public void Handler_推送异常时_应仅记日志不抛出()
    {
        _connectionManager.Setup(m => m.GetConnectionsAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Redis 不可用"));

        Assert.DoesNotThrowAsync(async () =>
            await CreateHandler().Handler(new FriendshipCreatedEvent(RequesterId, ReceiverId)));

        // 通知已落库，推送失败不回滚
        _notificationRepository.Verify(r => r.AddAsync(It.IsAny<TweetNotification>()), Times.Once);
    }
}
