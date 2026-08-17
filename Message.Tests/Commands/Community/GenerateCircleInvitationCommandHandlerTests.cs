using Message.Domain.Entities.Community;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Community;
using Message.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Message.Tests.Commands.Community;

/// <summary>
/// 生成圈子邀请命令处理程序单元测试（邀请机制完善）：
/// 覆盖：邀请码周额度（圈主不限/管理员2/普通1，滚动7天）、链接/直邀仅圈主、非有效成员拒绝。
/// </summary>
[TestFixture]
public class GenerateCircleInvitationCommandHandlerTests
{
    private static readonly Guid CircleGuid = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();

    private Mock<ICircleRepository> _circleRepository = null!;
    private Mock<ICircleInvitationRepository> _invitationRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;

    [SetUp]
    public void Setup()
    {
        _circleRepository = new Mock<ICircleRepository>();
        _invitationRepository = new Mock<ICircleInvitationRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();

        // 圈子：Owner 创建
        _circleRepository.Setup(r => r.GetByIdAsync(CircleGuid))
            .ReturnsAsync(Circle.Create(OwnerId, "测试圈子"));
        _invitationRepository.Setup(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _invitationRepository.Setup(r => r.CodeExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _invitationRepository.Setup(r => r.AddAsync(It.IsAny<CircleInvitation>()))
            .ReturnsAsync((CircleInvitation i) => i);
    }

    private void SetupMember(Guid userId, CircleMemberRole role, CircleMemberStatus status = CircleMemberStatus.Active)
    {
        var member = new CircleMember(CircleGuid, userId, role);
        if (status == CircleMemberStatus.Left)
            member.MarkLeft();
        else if (status == CircleMemberStatus.Banned)
            member.MarkBanned();
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, userId))
            .ReturnsAsync(member);
    }

    private void SetupCodeCount(int count)
    {
        _invitationRepository.Setup(r => r.CountCodesCreatedSinceAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(count);
    }

    private GenerateCircleInvitationCommandHandler CreateHandler()
    {
        var options = new Mock<IOptionsSnapshot<GenerateCirecleInvitationOption>>();
        options.Setup(o => o.Value).Returns(new GenerateCirecleInvitationOption());

        return new(_circleRepository.Object, _invitationRepository.Object,
            options.Object,
            new Mock<ILogger<GenerateCircleInvitationCommandHandler>>().Object);
    }

    // ---------- 邀请码额度 ----------

    [Test]
    public async Task GenerateCode_圈主_已创建大量邀请码_仍可生成()
    {
        SetupMember(OwnerId, CircleMemberRole.Owner);
        SetupCodeCount(100);

        var result = await CreateHandler().Handler(
            new GenerateCircleInvitationCommand(OwnerId, CircleGuid, "code", null, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.InviteGuid, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.Code, Is.Not.Null.And.Length.EqualTo(6));
        });
    }

    [Test]
    public async Task GenerateCode_管理员_未超限_可生成()
    {
        SetupMember(AdminId, CircleMemberRole.Admin);
        SetupCodeCount(1);

        var result = await CreateHandler().Handler(
            new GenerateCircleInvitationCommand(AdminId, CircleGuid, "code", null, null), CancellationToken.None);

        Assert.That(result.Code, Is.Not.Null.And.Length.EqualTo(6));
    }

    [Test]
    public async Task GenerateCode_管理员_已达每周2个额度_抛出InvalidOperationException()
    {
        SetupMember(AdminId, CircleMemberRole.Admin);
        SetupCodeCount(2);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(AdminId, CircleGuid, "code", null, null), CancellationToken.None));
    }

    [Test]
    public async Task GenerateCode_普通用户_未超限_可生成()
    {
        SetupMember(MemberId, CircleMemberRole.Member);
        SetupCodeCount(0);

        var result = await CreateHandler().Handler(
            new GenerateCircleInvitationCommand(MemberId, CircleGuid, "code", null, null), CancellationToken.None);

        Assert.That(result.Code, Is.Not.Null.And.Length.EqualTo(6));
    }

    [Test]
    public async Task GenerateCode_普通用户_已达每周1个额度_抛出InvalidOperationException()
    {
        SetupMember(MemberId, CircleMemberRole.Member);
        SetupCodeCount(1);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(MemberId, CircleGuid, "code", null, null), CancellationToken.None));
    }

    // ---------- 链接/直邀权限 ----------

    [Test]
    public async Task GenerateLink_普通用户_抛出UnauthorizedAccessException()
    {
        SetupMember(MemberId, CircleMemberRole.Member);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(MemberId, CircleGuid, "link", null, null), CancellationToken.None));
    }

    [Test]
    public async Task GenerateDirect_管理员_抛出UnauthorizedAccessException()
    {
        SetupMember(AdminId, CircleMemberRole.Admin);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(AdminId, CircleGuid, "direct", Guid.NewGuid(), null), CancellationToken.None));
    }

    [Test]
    public async Task GenerateLink_圈主_可生成且次数不限()
    {
        SetupMember(OwnerId, CircleMemberRole.Owner);

        var result = await CreateHandler().Handler(
            new GenerateCircleInvitationCommand(OwnerId, CircleGuid, "link", null, null), CancellationToken.None);

        Assert.That(result.Token, Is.Not.Null);
    }

    [Test]
    public async Task GenerateDirect_圈主_可生成()
    {
        SetupMember(OwnerId, CircleMemberRole.Owner);
        var invitee = Guid.NewGuid();

        var result = await CreateHandler().Handler(
            new GenerateCircleInvitationCommand(OwnerId, CircleGuid, "direct", invitee, null), CancellationToken.None);

        Assert.That(result.InviteGuid, Is.Not.EqualTo(Guid.Empty));
    }

    // ---------- 成员资格 ----------

    [Test]
    public async Task GenerateCode_非成员_抛出UnauthorizedAccessException()
    {
        _circleRepository.Setup(r => r.GetMemberAsync(CircleGuid, MemberId)).ReturnsAsync((CircleMember?)null);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(MemberId, CircleGuid, "code", null, null), CancellationToken.None));
    }

    [Test]
    public async Task GenerateCode_已退出成员_抛出UnauthorizedAccessException()
    {
        SetupMember(MemberId, CircleMemberRole.Member, CircleMemberStatus.Left);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await CreateHandler().Handler(
                new GenerateCircleInvitationCommand(MemberId, CircleGuid, "code", null, null), CancellationToken.None));
    }
}
