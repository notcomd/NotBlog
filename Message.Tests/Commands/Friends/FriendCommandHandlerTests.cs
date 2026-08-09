using Message.Domain.Entities.Chat;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Friends;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Friends;

/// <summary>
/// 好友命令处理程序单元测试。
/// 覆盖：发送好友请求（返回新关系 ID）、删除好友（关系不存在时抛出 KeyNotFoundException）。
/// </summary>
[TestFixture]
public class FriendCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FriendId = Guid.NewGuid();

    private Mock<IMessageFriendsRepository> _friendRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _friendRepository = new Mock<IMessageFriendsRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _friendRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
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

    // ---------- DeleteFriendCommandHandler ----------

    [Test]
    public async Task DeleteFriend_关系存在时_应删除友谊关系并返回true()
    {
        var friendship = new MessageFriends(UserId, FriendId);
        _friendRepository.Setup(r => r.GetByUserAndFriendAsync(UserId, FriendId)).ReturnsAsync(friendship);
        _friendRepository.Setup(r => r.DeleteAsync(friendship.FriendshipId)).Returns(Task.CompletedTask);

        var handler = new DeleteFriendCommandHandler(
            _friendRepository.Object,
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
            new Mock<ILogger<DeleteFriendCommandHandler>>().Object);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await handler.Handler(new DeleteFriendCommand(UserId, FriendId), CancellationToken.None));

        _friendRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}