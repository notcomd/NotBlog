using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Message.Domain.SeedWork;
using Message.Web.API.Application.Commands.Groups;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Groups;

/// <summary>
/// 群组命令权限负向测试（S-04）。
/// 覆盖：解散仅群主、设置管理员需群主/管理员、转让群主仅群主；
/// 权限不足时必须抛出 <see cref="UnauthorizedAccessException"/>，合法操作正常通过。
/// </summary>
[TestFixture]
public class GroupCommandPermissionTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid MemberId = Guid.NewGuid();
    private static readonly Guid TargetId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    private Mock<IGroupRepository> _groupRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ICurrentUserService> _currentUser = null!;

    [SetUp]
    public void Setup()
    {
        _groupRepository = new Mock<IGroupRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _groupRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        _groupRepository.Setup(r => r.UpdateAsync(It.IsAny<Group>()))
            .ReturnsAsync((Group g) => g);
    }

    /// <summary>以指定用户身份构建一个含 Owner/Admin/Member 三角色的群组</summary>
    private Group BuildGroup(Guid actingUserId)
    {
        var group = new Group(OwnerId, "测试群", 500, false);
        group.AddMember(AdminId, GroupMemberRole.Admin);
        group.AddMember(MemberId, GroupMemberRole.Member);
        group.AddMember(TargetId, GroupMemberRole.Member);
        _groupRepository.Setup(r => r.GetByIdWithMembersAsync(GroupId)).ReturnsAsync(group);
        _currentUser.Setup(c => c.GetUserId()).Returns(actingUserId);
        return group;
    }

    // ---------- DismissGroupCommandHandler：仅 Owner ----------

    [Test]
    public async Task DismissGroup_普通成员解散他人群_应抛出UnauthorizedAccessException()
    {
        BuildGroup(MemberId);

        var handler = new DismissGroupCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<DismissGroupCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new DismissGroupCommand(GroupId), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DismissGroup_管理员解散他人群_应抛出UnauthorizedAccessException()
    {
        BuildGroup(AdminId);

        var handler = new DismissGroupCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<DismissGroupCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new DismissGroupCommand(GroupId), CancellationToken.None));
    }

    [Test]
    public async Task DismissGroup_群主解散群_应返回true()
    {
        var group = BuildGroup(OwnerId);

        var handler = new DismissGroupCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<DismissGroupCommandHandler>>().Object);

        var result = await handler.Handler(new DismissGroupCommand(GroupId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(group.IsDismissed, Is.True);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- SetAdminCommandHandler：需 Owner/Admin ----------

    [Test]
    public async Task SetAdmin_普通成员将他人设为管理员_应抛出UnauthorizedAccessException()
    {
        BuildGroup(MemberId);

        var handler = new SetAdminCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<SetAdminCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new SetAdminCommand(GroupId, TargetId, true), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SetAdmin_管理员将成员设为管理员_应返回true()
    {
        var group = BuildGroup(AdminId);

        var handler = new SetAdminCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<SetAdminCommandHandler>>().Object);

        var result = await handler.Handler(new SetAdminCommand(GroupId, TargetId, true), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(group.GetMember(TargetId)!.Role, Is.EqualTo(GroupMemberRole.Admin));
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    // ---------- TransferOwnershipCommandHandler：仅 Owner ----------

    [Test]
    public async Task TransferOwnership_非群主转让群主_应抛出UnauthorizedAccessException()
    {
        BuildGroup(AdminId);

        var handler = new TransferOwnershipCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<TransferOwnershipCommandHandler>>().Object);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handler(new TransferOwnershipCommand(GroupId, TargetId), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task TransferOwnership_群主转让群主_应返回true()
    {
        var group = BuildGroup(OwnerId);

        var handler = new TransferOwnershipCommandHandler(
            _groupRepository.Object, _currentUser.Object,
            new Mock<ILogger<TransferOwnershipCommandHandler>>().Object);

        var result = await handler.Handler(new TransferOwnershipCommand(GroupId, TargetId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(group.OwnerId, Is.EqualTo(TargetId));
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }
}
