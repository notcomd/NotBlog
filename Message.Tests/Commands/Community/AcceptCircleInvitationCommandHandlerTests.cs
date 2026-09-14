using Message.Domain.Entities.Community;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Community;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Community;

/// <summary>
/// 接受直邀命令处理程序单元测试：
/// 覆盖：确认后加入圈子并物理删除邀请（已用直邀不留存）、非直邀/非本人/已撤销的校验。
/// </summary>
[TestFixture]
public class AcceptCircleInvitationCommandHandlerTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid InviteeId = Guid.NewGuid();

    private Mock<ICircleRepository> _circleRepository = null!;
    private Mock<ICircleInvitationRepository> _invitationRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Circle _circle = null!;
    private CircleInvitation _directInvitation = null!;

    [SetUp]
    public void Setup()
    {
        _circleRepository = new Mock<ICircleRepository>();
        _invitationRepository = new Mock<ICircleInvitationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _circle = Circle.Create(OwnerId, "测试圈子");
        _directInvitation = CircleInvitation.CreateDirect(_circle.CircleGuid, OwnerId, InviteeId);

        _circleRepository.Setup(r => r.GetByIdWithMembersAsync(_circle.CircleGuid)).ReturnsAsync(_circle);
        _circleRepository.Setup(r => r.UpdateAsync(It.IsAny<Circle>())).ReturnsAsync((Circle c) => c);
        _circleRepository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _invitationRepository.Setup(r => r.GetByIdAsync(_directInvitation.InviteGuid)).ReturnsAsync(_directInvitation);
    }

    private AcceptCircleInvitationCommandHandler CreateHandler()
        => new(_circleRepository.Object, _invitationRepository.Object,
            new Mock<ILogger<AcceptCircleInvitationCommandHandler>>().Object);

    [Test]
    public async Task 接受直邀_加入圈子并物理删除邀请()
    {
        var result = await CreateHandler().Handler(
            new AcceptCircleInvitationCommand(InviteeId, _directInvitation.InviteGuid), CancellationToken.None);

        Assert.That(result, Is.EqualTo(_circle.CircleGuid));
        Assert.That(_circle.IsMember(InviteeId), Is.True);
        _invitationRepository.Verify(r => r.DeleteAsync(_directInvitation.InviteGuid), Times.Once,
            "直邀确认后必须物理删除，避免已用邀请残留在邀请列表");
        _invitationRepository.Verify(r => r.UpdateAsync(It.IsAny<CircleInvitation>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void 非直邀_抛出InvalidOperationException()
    {
        var codeInvitation = CircleInvitation.CreateCode(_circle.CircleGuid, OwnerId, "ABC123");
        _invitationRepository.Setup(r => r.GetByIdAsync(codeInvitation.InviteGuid)).ReturnsAsync(codeInvitation);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(
                new AcceptCircleInvitationCommand(InviteeId, codeInvitation.InviteGuid), CancellationToken.None));

        _invitationRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void 邀请不是发给当前用户_抛出UnauthorizedAccessException()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler().Handler(
                new AcceptCircleInvitationCommand(Guid.NewGuid(), _directInvitation.InviteGuid), CancellationToken.None));

        _invitationRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public void 邀请已撤销_抛出InvalidOperationException且不删除()
    {
        _directInvitation.Revoke(OwnerId);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(
                new AcceptCircleInvitationCommand(InviteeId, _directInvitation.InviteGuid), CancellationToken.None));

        _invitationRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }
}
