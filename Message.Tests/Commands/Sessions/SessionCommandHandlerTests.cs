using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Domain.SeedWork;
using Message.Tests.TestHelpers;
using Message.Web.API.Application.Commands.Sessions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Sessions;

/// <summary>
/// 会话命令处理程序单元测试。
/// 覆盖：创建会话（返回新会话 ID）、置顶/取消置顶、添加参与者、解散会话。
/// 验证 CQRS 命令侧仅返回操作结果（Guid/bool），不返回业务实体。
/// </summary>
[TestFixture]
public class SessionCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid FriendId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();

    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ICurrentUserService> _currentUser = null!;

    [SetUp]
    public void Setup()
    {
        _sessionRepository = new Mock<IChatSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(c => c.GetUserId()).Returns(UserId);
        _sessionRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
    }

    // ---------- CreateSessionCommandHandler ----------

    [Test]
    public async Task CreateSession_私聊不存在时_应创建并持久化新会话()
    {
        ChatSession? added = null;
        _sessionRepository.Setup(r => r.GetPrivateSessionAsync(UserId, FriendId))
            .ReturnsAsync((ChatSession?)null);
        _sessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => added = s)
            .ReturnsAsync((ChatSession s) => s);

        var handler = new CreateSessionCommandHandler(
            _sessionRepository.Object,
            new Mock<ILogger<CreateSessionCommandHandler>>().Object);

        var result = await handler.Handler(
            new CreateSessionCommand(UserId, SessionType.Private, FriendId, null, null, null),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.SessionId));
            _sessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task CreateSession_私聊已存在时_应返回已有会话ID且不重复创建()
    {
        var existing = new ChatSession(SessionType.Private, UserId, new HashSet<Guid> { UserId, FriendId });
        _sessionRepository.Setup(r => r.GetPrivateSessionAsync(UserId, FriendId)).ReturnsAsync(existing);

        var handler = new CreateSessionCommandHandler(
            _sessionRepository.Object,
            new Mock<ILogger<CreateSessionCommandHandler>>().Object);

        var result = await handler.Handler(
            new CreateSessionCommand(UserId, SessionType.Private, FriendId, null, null, null),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(existing.SessionId));
            _sessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        });
    }

    [Test]
    public async Task CreateSession_群聊_应创建群聊会话并持久化()
    {
        ChatSession? added = null;
        _sessionRepository.Setup(r => r.AddAsync(It.IsAny<ChatSession>()))
            .Callback<ChatSession>(s => added = s)
            .ReturnsAsync((ChatSession s) => s);

        var handler = new CreateSessionCommandHandler(
            _sessionRepository.Object,
            new Mock<ILogger<CreateSessionCommandHandler>>().Object);

        var result = await handler.Handler(
            new CreateSessionCommand(UserId, SessionType.Group, null, Guid.NewGuid(), "测试群",
                new HashSet<Guid> { UserId, FriendId }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.SessionId));
            Assert.That(added.SessionType, Is.EqualTo(SessionType.Group));
            _sessionRepository.Verify(r => r.AddAsync(It.IsAny<ChatSession>()), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- SetSessionPinCommandHandler ----------

    [Test]
    public async Task SetSessionPin_置顶时_应调用Pin并保存()
    {
        var session = new ChatSession(SessionType.Private, UserId, new HashSet<Guid> { UserId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _sessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        var handler = new SetSessionPinCommandHandler(
            _sessionRepository.Object,
            _currentUser.Object,
            new Mock<ILogger<SetSessionPinCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(new SetSessionPinCommand(SessionId, true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsPinned, Is.True);
            _sessionRepository.Verify(r => r.UpdateAsync(session), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task SetSessionPin_取消置顶时_应调用Unpin并保存()
    {
        var session = new ChatSession(SessionType.Private, UserId, new HashSet<Guid> { UserId });
        session.Pin();
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _sessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        var handler = new SetSessionPinCommandHandler(
            _sessionRepository.Object,
            _currentUser.Object,
            new Mock<ILogger<SetSessionPinCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(new SetSessionPinCommand(SessionId, false), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsPinned, Is.False);
            _sessionRepository.Verify(r => r.UpdateAsync(session), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task SetSessionPin_会话不存在时_应抛出KeyNotFoundException()
    {
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync((ChatSession?)null);

        var handler = new SetSessionPinCommandHandler(
            _sessionRepository.Object,
            _currentUser.Object,
            new Mock<ILogger<SetSessionPinCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await handler.Handler(new SetSessionPinCommand(SessionId, true), CancellationToken.None));
    }

    // ---------- AddSessionParticipantCommandHandler ----------

    [Test]
    public async Task AddParticipant_应添加参与者并返回true()
    {
        var session = new ChatSession(SessionType.Private, UserId, new HashSet<Guid> { UserId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _sessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        var handler = new AddSessionParticipantCommandHandler(
            _sessionRepository.Object,
            _currentUser.Object,
            new Mock<ILogger<AddSessionParticipantCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(
            new AddSessionParticipantCommand(SessionId, FriendId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsParticipant(FriendId), Is.True);
            _sessionRepository.Verify(r => r.UpdateAsync(session), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- DismissSessionCommandHandler ----------

    [Test]
    public async Task DismissSession_应解散会话并返回true()
    {
        var session = new ChatSession(SessionType.Private, UserId, new HashSet<Guid> { UserId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _sessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);

        var handler = new DismissSessionCommandHandler(
            _sessionRepository.Object,
            _currentUser.Object,
            new Mock<ILogger<DismissSessionCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(new DismissSessionCommand(SessionId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsDismissed, Is.True);
            _sessionRepository.Verify(r => r.UpdateAsync(session), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}
