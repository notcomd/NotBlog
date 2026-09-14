using Message.Domain.Entities.Chat;
using Message.Domain.Enums;
using Message.Domain.Events;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Friends;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Friends;

/// <summary>
/// 好友命令处理程序单元测试。
/// 覆盖：发送好友请求（返回新关系 ID + 发送前校验分支）、删除好友（关系不存在时抛出 KeyNotFoundException）。
/// </summary>
[TestFixture]
public class FriendCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FriendId = Guid.NewGuid();

    private Mock<IMessageFriendsRepository> _friendRepository = null!;
    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _friendRepository = new Mock<IMessageFriendsRepository>();
        _sessionRepository = new Mock<IChatSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _friendRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        // 默认：不存在私聊会话（联动逻辑跳过）
        _sessionRepository.Setup(r => r.GetPrivateSessionAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync((ChatSession?)null);
    }

    // ---------- SendFriendRequestCommandHandler ----------

    [Test]
    public async Task SendFriendRequest_应创建好友关系并返回新ID()
    {
        MessageFriends? added = null;
        _friendRepository.Setup(r => r.AddAsync(It.IsAny<MessageFriends>()))
            .Callback<MessageFriends>(f => added = f)
            .ReturnsAsync((MessageFriends f) => f);

        var handler = new SendFriendRequestCommandHandler(
            _friendRepository.Object,
            new Mock<ILogger<SendFriendRequestCommandHandler>>().Object);

        var result = await handler.Handler(new SendFriendRequestCommand(UserId, FriendId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.FriendshipId));
            _friendRepository.Verify(r => r.AddAsync(
                It.Is<MessageFriends>(f => f.UserId == UserId && f.FriendId == FriendId)), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- 发送前校验 ----------

    [Test]
    public void SendFriendRequest_添加自己_应抛业务异常()
    {
        var handler = new SendFriendRequestCommandHandler(
            _friendRepository.Object,
            new Mock<ILogger<SendFriendRequestCommandHandler>>().Object);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(new SendFriendRequestCommand(UserId, UserId), CancellationToken.None));

        _friendRepository.Verify(r => r.AddAsync(It.IsAny<MessageFriends>()), Times.Never);
    }

    [Test]
    public async Task SendFriendRequest_已是好友_应抛业务异常()
    {
        var friendship = new MessageFriends(UserId, FriendId);
        friendship.Accept();
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(UserId, FriendId)).ReturnsAsync(friendship);

        var handler = new SendFriendRequestCommandHandler(
            _friendRepository.Object,
            new Mock<ILogger<SendFriendRequestCommandHandler>>().Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(new SendFriendRequestCommand(UserId, FriendId), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(ex!.Message, Is.EqualTo("对方已经是您的好友"));
            _friendRepository.Verify(r => r.AddAsync(It.IsAny<MessageFriends>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Test]
    public async Task SendFriendRequest_同向已有待处理请求_应抛业务异常()
    {
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(UserId, FriendId))
            .ReturnsAsync(new MessageFriends(UserId, FriendId));

        var handler = new SendFriendRequestCommandHandler(
            _friendRepository.Object,
            new Mock<ILogger<SendFriendRequestCommandHandler>>().Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(new SendFriendRequestCommand(UserId, FriendId), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("好友请求已发送，等待对方处理"));
    }

    [Test]
    public async Task SendFriendRequest_反向已有待处理请求_应抛业务异常()
    {
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(FriendId, UserId))
            .ReturnsAsync(new MessageFriends(FriendId, UserId));

        var handler = new SendFriendRequestCommandHandler(
            _friendRepository.Object,
            new Mock<ILogger<SendFriendRequestCommandHandler>>().Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handler(new SendFriendRequestCommand(UserId, FriendId), CancellationToken.None));

        Assert.That(ex!.Message, Is.EqualTo("对方已向您发送好友请求，请直接同意"));
    }

    // ---------- 领域事件 ----------

    [Test]
    public void MessageFriends_创建时_应发布FriendshipCreatedEvent()
    {
        var friendship = new MessageFriends(UserId, FriendId);

        var @event = friendship.DomainEvents.OfType<FriendshipCreatedEvent>().SingleOrDefault();

        Assert.Multiple(() =>
        {
            Assert.That(friendship.Status, Is.EqualTo(FriendshipStatus.Pending));
            Assert.That(@event, Is.Not.Null);
            Assert.That(@event!.UserId, Is.EqualTo(UserId));
            Assert.That(@event.FriendId, Is.EqualTo(FriendId));
        });
    }

    // ---------- DeleteFriendCommandHandler ----------

    [Test]
    public async Task DeleteFriend_关系存在时_应删除友谊关系并返回true()
    {
        var friendship = new MessageFriends(UserId, FriendId);
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(UserId, FriendId)).ReturnsAsync(friendship);
        _friendRepository.Setup(r => r.DeleteAsync(friendship.FriendshipId)).Returns(Task.CompletedTask);

        var handler = new DeleteFriendCommandHandler(
            _friendRepository.Object,
            _sessionRepository.Object,
            new Mock<ILogger<DeleteFriendCommandHandler>>().Object);

        var result = await handler.Handler(new DeleteFriendCommand(UserId, FriendId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            _friendRepository.Verify(r => r.DeleteAsync(friendship.FriendshipId), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public void DeleteFriend_关系不存在时_应抛出KeyNotFoundException()
    {
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(UserId, FriendId))
            .ReturnsAsync((MessageFriends?)null);

        var handler = new DeleteFriendCommandHandler(
            _friendRepository.Object,
            _sessionRepository.Object,
            new Mock<ILogger<DeleteFriendCommandHandler>>().Object);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await handler.Handler(new DeleteFriendCommand(UserId, FriendId), CancellationToken.None));

        _friendRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}