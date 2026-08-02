using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Tests.TestHelpers;
using Message.Web.API.Application.Queries.Sessions;
using Moq;

namespace Message.Tests.Queries.Sessions;

/// <summary>
/// 会话查询处理程序单元测试。
/// 覆盖：会话详情透传、用户会话列表、未读总数。
/// 验证 CQRS 查询侧仅返回只读结果，不触发任何写操作。
/// </summary>
[TestFixture]
public class SessionQueryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid SessionId = Guid.NewGuid();

    private Mock<IChatSessionRepository> _sessionRepository = null!;
    private Mock<ICurrentUserService> _currentUser = null!;

    [SetUp]
    public void Setup()
    {
        _sessionRepository = new Mock<IChatSessionRepository>();
        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(c => c.GetUserId()).Returns(UserId);
    }

    // ---------- GetSessionQueryHandler ----------

    [Test]
    public async Task GetSession_会话存在时_应透传会话实体()
    {
        var session = new ChatSession(SessionType.Private, UserId);
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);

        var handler = new GetSessionQueryHandler(_sessionRepository.Object, _currentUser.Object);

        var result = await handler.Handler(new GetSessionQuery(SessionId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(session));
            _sessionRepository.Verify(r => r.GetByIdAsync(SessionId), Times.Once);
        });
    }

    [Test]
    public async Task GetSession_会话不存在时_应返回null()
    {
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync((ChatSession?)null);

        var handler = new GetSessionQueryHandler(_sessionRepository.Object, _currentUser.Object);

        var result = await handler.Handler(new GetSessionQuery(SessionId), CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetSession_非参与者读取他人会话_应返回null()
    {
        // 会话参与者仅 OtherUser，当前用户（UserId）非参与者
        var otherUser = Guid.NewGuid();
        var session = new ChatSession(SessionType.Private, otherUser, new HashSet<Guid> { otherUser });
        _sessionRepository.Setup(r => r.GetByIdAsync(SessionId)).ReturnsAsync(session);

        var handler = new GetSessionQueryHandler(_sessionRepository.Object, _currentUser.Object);

        var result = await handler.Handler(new GetSessionQuery(SessionId), CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    // ---------- GetUserSessionsQueryHandler ----------

    [Test]
    public async Task GetUserSessions_应返回用户会话列表()
    {
        var sessions = new List<ChatSession> { new(SessionType.Private, UserId) };
        _sessionRepository.Setup(r => r.GetByUserIdAsync(UserId)).ReturnsAsync(sessions);

        var handler = new GetUserSessionsQueryHandler(_sessionRepository.Object);

        var result = await handler.Handler(new GetUserSessionsQuery(UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(sessions));
            _sessionRepository.Verify(r => r.GetByUserIdAsync(UserId), Times.Once);
        });
    }

    // ---------- GetTotalUnreadCountQueryHandler ----------

    [Test]
    public async Task GetTotalUnreadCount_应返回未读消息总数()
    {
        _sessionRepository.Setup(r => r.GetTotalUnreadCountAsync(UserId)).ReturnsAsync(42);

        var handler = new GetTotalUnreadCountQueryHandler(_sessionRepository.Object,
            CacheServicesTestFactory.CreateUnreadCountCache());

        var result = await handler.Handler(new GetTotalUnreadCountQuery(UserId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(42));
            _sessionRepository.Verify(r => r.GetTotalUnreadCountAsync(UserId), Times.Once);
        });
    }
}
