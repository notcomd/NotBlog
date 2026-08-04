using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Tests.TestHelpers;
using Message.Web.API.Application.Commands.Sessions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Sessions;

/// <summary>
/// 会话 IDOR 负向测试（S-05）。
/// 覆盖：非参与者不得置顶/解散会话，参与者操作正常通过。
/// </summary>
[TestFixture]
public class SessionIdorTests
{
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly Guid ParticipantId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ICurrentUserService> _currentUser = null!;

    [SetUp]
    public void Setup()
    {
        _sessionRepository = new Mock<IChatSessionRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _sessionRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _sessionRepository.Setup(r => r.UpdateAsync(It.IsAny<ChatSession>()))
            .ReturnsAsync((ChatSession s) => s);
    }

    private ChatSession BuildSession(Guid actingUserId)
    {
        var session = new ChatSession(SessionType.Private, ParticipantId, new HashSet<Guid> { ParticipantId });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);
        _currentUser.Setup(c => c.GetUserId()).Returns(actingUserId);
        return session;
    }

    // ---------- SetSessionPinCommandHandler ----------

    [Test]
    public async Task SetSessionPin_非参与者置顶他人会话_应抛出UnauthorizedAccessException()
    {
        BuildSession(OutsiderId);

        var handler = new SetSessionPinCommandHandler(
            _sessionRepository.Object, _currentUser.Object,
            new Mock<ILogger<SetSessionPinCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new SetSessionPinCommand(SessionId, true), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetSessionPin_参与者置顶会话_应返回true()
    {
        var session = BuildSession(ParticipantId);

        var handler = new SetSessionPinCommandHandler(
            _sessionRepository.Object, _currentUser.Object,
            new Mock<ILogger<SetSessionPinCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(new SetSessionPinCommand(SessionId, true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsPinned, Is.True);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- DismissSessionCommandHandler ----------

    [Test]
    public async Task DismissSession_非参与者解散他人会话_应抛出UnauthorizedAccessException()
    {
        BuildSession(OutsiderId);

        var handler = new DismissSessionCommandHandler(
            _sessionRepository.Object, _currentUser.Object,
            new Mock<ILogger<DismissSessionCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new DismissSessionCommand(SessionId), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DismissSession_参与者解散会话_应返回true()
    {
        var session = BuildSession(ParticipantId);

        var handler = new DismissSessionCommandHandler(
            _sessionRepository.Object, _currentUser.Object,
            new Mock<ILogger<DismissSessionCommandHandler>>().Object,
            CacheServicesTestFactory.CreateSessionCache());

        var result = await handler.Handler(new DismissSessionCommand(SessionId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(session.IsDismissed, Is.True);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}
