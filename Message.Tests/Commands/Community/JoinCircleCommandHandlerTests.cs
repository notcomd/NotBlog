using Message.Domain.Entities.Community;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Community;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Community;

/// <summary>
/// 凭邀请加入圈子命令处理程序单元测试（邀请机制完善）：
/// 覆盖：邀请一次性使用（原子占用失败即失效）、邀请不存在、token 路径、码优先、用后物理删除。
/// </summary>
[TestFixture]
public class JoinCircleCommandHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid JoinerId = Guid.NewGuid();

    private Mock<ICircleRepository> _circleRepository = null!;
    private Mock<ICircleInvitationRepository> _invitationRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Circle _circle = null!;
    private CircleInvitation _codeInvitation = null!;

    [SetUp]
    public void Setup()
    {
        _circleRepository = new Mock<ICircleRepository>();
        _invitationRepository = new Mock<ICircleInvitationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // 实体 Guid 由工厂生成（CreateVersion7），断言一律用 mock 对象的真实 Guid
        _circle = Circle.Create(OwnerId, "测试圈子");
        _codeInvitation = CircleInvitation.CreateCode(_circle.CircleGuid, OwnerId, "ABC123");

        _circleRepository.Setup(r => r.GetByIdWithMembersAsync(_circle.CircleGuid)).ReturnsAsync(_circle);
        _circleRepository.Setup(r => r.UpdateAsync(It.IsAny<Circle>()))
            .ReturnsAsync((Circle c) => c);
        _circleRepository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _invitationRepository.Setup(r => r.UpdateAsync(It.IsAny<CircleInvitation>()))
            .ReturnsAsync((CircleInvitation i) => i);
    }

    private JoinCircleCommandHandler CreateHandler()
        => new(_circleRepository.Object, _invitationRepository.Object,
            new Mock<ILogger<JoinCircleCommandHandler>>().Object);

    // ---------- 邀请码加入 ----------

    [Test]
    public async Task Join_有效邀请码_原子占用成功_返回圈子ID()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(_codeInvitation);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid)).ReturnsAsync(true);

        var result = await CreateHandler().Handler(
            new JoinCircleCommand(JoinerId, "ABC123", null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(_circle.CircleGuid));
            _invitationRepository.Verify(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid), Times.Once);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task Join_邀请已被使用_原子占用失败_抛出InvalidOperationException()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(_codeInvitation);
        // 并发/重复使用场景：条件更新 0 行
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid)).ReturnsAsync(false);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(
                new JoinCircleCommand(JoinerId, "ABC123", null), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Join_邀请不存在_抛出KeyNotFoundException()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ZZZZZZ")).ReturnsAsync((CircleInvitation?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await CreateHandler().Handler(
                new JoinCircleCommand(JoinerId, "ZZZZZZ", null), CancellationToken.None));
    }

    // ---------- 链接 token 加入 ----------

    [Test]
    public async Task Join_有效链接token_加入成功()
    {
        var link = CircleInvitation.CreateLink(_circle.CircleGuid, OwnerId);
        _invitationRepository.Setup(r => r.GetByTokenAsync(link.Token!.Value)).ReturnsAsync(link);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(link.InviteGuid)).ReturnsAsync(true);

        var result = await CreateHandler().Handler(
            new JoinCircleCommand(JoinerId, null, link.Token), CancellationToken.None);

        Assert.That(result, Is.EqualTo(_circle.CircleGuid));
    }

    [Test]
    public async Task Join_同时提供码和token_取码优先()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(_codeInvitation);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid)).ReturnsAsync(true);

        var result = await CreateHandler().Handler(
            new JoinCircleCommand(JoinerId, "ABC123", Guid.NewGuid()), CancellationToken.None);

        Assert.That(result, Is.EqualTo(_circle.CircleGuid));
        _invitationRepository.Verify(r => r.GetByCodeAsync("ABC123"), Times.Once);
    }

    // ---------- 用后物理删除（已用邀请不残留） ----------

    [Test]
    public async Task Join_邀请码使用后_立即物理删除()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(_codeInvitation);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid)).ReturnsAsync(true);

        await CreateHandler().Handler(new JoinCircleCommand(JoinerId, "ABC123", null), CancellationToken.None);

        _invitationRepository.Verify(r => r.DeleteAsync(_codeInvitation.InviteGuid), Times.Once,
            "邀请码使用后必须物理删除，避免已用邀请残留在邀请列表");
        _invitationRepository.Verify(r => r.UpdateAsync(It.IsAny<CircleInvitation>()), Times.Never,
            "不应再以状态置位方式保留已用邀请");
    }

    [Test]
    public async Task Join_链接使用后_立即物理删除()
    {
        var link = CircleInvitation.CreateLink(_circle.CircleGuid, OwnerId);
        _invitationRepository.Setup(r => r.GetByTokenAsync(link.Token!.Value)).ReturnsAsync(link);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(link.InviteGuid)).ReturnsAsync(true);

        await CreateHandler().Handler(new JoinCircleCommand(JoinerId, null, link.Token), CancellationToken.None);

        _invitationRepository.Verify(r => r.DeleteAsync(link.InviteGuid), Times.Once);
    }

    [Test]
    public async Task Join_原子占用失败_不删除邀请也不入圈()
    {
        _invitationRepository.Setup(r => r.GetByCodeAsync("ABC123")).ReturnsAsync(_codeInvitation);
        _invitationRepository.Setup(r => r.TryAcceptAtomicallyAsync(_codeInvitation.InviteGuid)).ReturnsAsync(false);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(new JoinCircleCommand(JoinerId, "ABC123", null), CancellationToken.None));

        _invitationRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        Assert.That(_circle.IsMember(JoinerId), Is.False);
    }
}
