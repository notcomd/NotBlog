using Message.Domain.Entities.Community;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Community;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Community;

/// <summary>
/// 撤销圈子邀请命令处理程序单元测试（撤销权限判定）：
/// 覆盖：有效圈主/管理员可撤销；已退出（Left）/被移出（Banned）的历史管理员不可撤销；
/// 普通成员不可撤销；直邀被邀请人可拒绝（非成员亦可）。
/// </summary>
[TestFixture]
public class RevokeCircleInvitationCommandHandlerTests
{
    private static readonly Guid CircleGuid = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid OutsiderId = Guid.NewGuid();

    private Mock<ICircleRepository> _circleRepository = null!;
    private Mock<ICircleInvitationRepository> _invitationRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _circleRepository = new Mock<ICircleRepository>();
        _invitationRepository = new Mock<ICircleInvitationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        _invitationRepository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _invitationRepository.Setup(r => r.UpdateAsync(It.IsAny<CircleInvitation>()))
            .ReturnsAsync((CircleInvitation i) => i);
    }

    /// <summary>构造待撤销的邀请码邀请（圈主发起）</summary>
    private CircleInvitation SetupCodeInvitation()
    {
        var invitation = CircleInvitation.CreateCode(CircleGuid, OwnerId, "ABC123");
        _invitationRepository.Setup(r => r.GetByIdAsync(invitation.InviteGuid)).ReturnsAsync(invitation);
        return invitation;
    }

    /// <summary>构造待撤销的直邀（被邀请人为 <paramref name="inviteeGuid"/>）</summary>
    private CircleInvitation SetupDirectInvitation(Guid inviteeGuid)
    {
        var invitation = CircleInvitation.CreateDirect(CircleGuid, OwnerId, inviteeGuid);
        _invitationRepository.Setup(r => r.GetByIdAsync(invitation.InviteGuid)).ReturnsAsync(invitation);
        return invitation;
    }

    private void SetupMember(Guid userId, CircleMemberRole role, CircleMemberStatus status = CircleMemberStatus.Active)
    {
        var member = new CircleMember(CircleGuid, userId, role: role);
        if (status == CircleMemberStatus.Left)
            member.MarkLeft();
        else if (status == CircleMemberStatus.Banned)
            member.MarkBanned();
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, userId)).ReturnsAsync(member);
    }

    private RevokeCircleInvitationCommandHandler CreateHandler() =>
        new(_invitationRepository.Object, _circleRepository.Object,
            new Mock<ILogger<RevokeCircleInvitationCommandHandler>>().Object);

    [Test]
    public async Task 有效管理员_可撤销邀请()
    {
        var invitation = SetupCodeInvitation();
        SetupMember(AdminId, CircleMemberRole.Admin);

        var result = await CreateHandler().Handler(new RevokeCircleInvitationCommand(AdminId, invitation.InviteGuid), default);

        Assert.That(result, Is.True);
        Assert.That(invitation.Status, Is.EqualTo(CircleInvitationStatus.Revoked));
    }

    [Test]
    public async Task 有效圈主_可撤销邀请()
    {
        var invitation = SetupCodeInvitation();
        SetupMember(OwnerId, CircleMemberRole.Owner);

        var result = await CreateHandler().Handler(new RevokeCircleInvitationCommand(OwnerId, invitation.InviteGuid), default);

        Assert.That(result, Is.True);
        Assert.That(invitation.Status, Is.EqualTo(CircleInvitationStatus.Revoked));
    }

    [Test]
    public void 已退出的前管理员_不可撤销邀请()
    {
        var invitation = SetupCodeInvitation();
        SetupMember(AdminId, CircleMemberRole.Admin, CircleMemberStatus.Left);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handler(new RevokeCircleInvitationCommand(AdminId, invitation.InviteGuid), default));
        Assert.That(invitation.Status, Is.EqualTo(CircleInvitationStatus.Pending), "撤销失败时邀请应保持待使用");
    }

    [Test]
    public void 被移出的前管理员_不可撤销邀请()
    {
        var invitation = SetupCodeInvitation();
        SetupMember(AdminId, CircleMemberRole.Admin, CircleMemberStatus.Banned);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handler(new RevokeCircleInvitationCommand(AdminId, invitation.InviteGuid), default));
        Assert.That(invitation.Status, Is.EqualTo(CircleInvitationStatus.Pending));
    }

    [Test]
    public void 普通成员_不可撤销邀请()
    {
        var invitation = SetupCodeInvitation();
        SetupMember(MemberId, CircleMemberRole.Member);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handler(new RevokeCircleInvitationCommand(MemberId, invitation.InviteGuid), default));
    }

    [Test]
    public void 非成员_不可撤销邀请()
    {
        var invitation = SetupCodeInvitation();

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CreateHandler().Handler(new RevokeCircleInvitationCommand(OutsiderId, invitation.InviteGuid), default));
    }

    [Test]
    public async Task 直邀被邀请人_可拒绝邀请_即使不是圈子成员()
    {
        var invitation = SetupDirectInvitation(OutsiderId);

        var result = await CreateHandler().Handler(new RevokeCircleInvitationCommand(OutsiderId, invitation.InviteGuid), default);

        Assert.That(result, Is.True);
        Assert.That(invitation.Status, Is.EqualTo(CircleInvitationStatus.Revoked));
    }
}
