using Message.Domain.Entities.Group;
using Message.Domain.Enums;
using Message.Domain.IRepository;
using Message.Domain.IServices;
using Commons.SeedWork;
using Message.Web.API.Application.Commands.Groups;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.Commands.Groups;

/// <summary>
/// 创建群组命令处理程序单元测试。
/// 覆盖：无初始成员直接创建、有初始成员时逐个添加成员。
/// </summary>
[TestFixture]
public class CreateGroupCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid MemberA = Guid.NewGuid();
    private static readonly Guid MemberB = Guid.NewGuid();

    private Mock<IGroupRepository> _groupRepository = null!;
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<ICurrentUserService> _currentUser = null!;
    private Mock<IUserInfoRepository> _userInfoRepository = null!;

    [SetUp]
    public void Setup()
    {
        _groupRepository = new Mock<IGroupRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _currentUser = new Mock<ICurrentUserService>();
        _currentUser.Setup(c => c.GetUserId()).Returns(UserId);
        _groupRepository.SetupGet(r => r.UnitOfWork).Returns(_unitOfWork.Object);
        // 等级挂钩（设计文档 4.5）：默认无资料按 1 级（群人数上限 30）
        _userInfoRepository = new Mock<IUserInfoRepository>();
        _userInfoRepository.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Message.Domain.Entities.User.UserInfo?)null);
    }

    private CreateGroupCommandHandler CreateHandler() => new(
        _groupRepository.Object, _currentUser.Object, _userInfoRepository.Object,
        new Mock<ILogger<CreateGroupCommandHandler>>().Object);

    [Test]
    public async Task CreateGroup_无初始成员时_应仅创建群组并返回群组ID()
    {
        Group? added = null;
        _groupRepository.Setup(r => r.AddAsync(It.IsAny<Group>()))
            .Callback<Group>(g => added = g)
            .ReturnsAsync((Group g) => g);

        var handler = CreateHandler();

        var result = await handler.Handler(
            new CreateGroupCommand(UserId, "测试群", 500, false, null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.GroupId));
            _groupRepository.Verify(r => r.AddAsync(It.IsAny<Group>()), Times.Once);
            _groupRepository.Verify(r => r.GetByIdWithMembersAsync(It.IsAny<Guid>()), Times.Never);
            _groupRepository.Verify(r => r.UpdateAsync(It.IsAny<Group>()), Times.Never);
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        });
    }

    [Test]
    public async Task CreateGroup_有初始成员时_应逐个加载并添加成员()
    {
        Group? added = null;
        _groupRepository.Setup(r => r.AddAsync(It.IsAny<Group>()))
            .Callback<Group>(g => added = g)
            .ReturnsAsync((Group g) => g);
        _groupRepository.Setup(r => r.GetByIdWithMembersAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => added!);
        _groupRepository.Setup(r => r.UpdateAsync(It.IsAny<Group>()))
            .ReturnsAsync((Group g) => g);

        var initialMembers = new HashSet<Guid> { MemberA, MemberB };
        var handler = CreateHandler();

        var result = await handler.Handler(
            new CreateGroupCommand(UserId, "测试群", 500, false, initialMembers), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(added!.GroupId));
            _groupRepository.Verify(r => r.GetByIdWithMembersAsync(added.GroupId), Times.Exactly(2));
            _groupRepository.Verify(r => r.UpdateAsync(added), Times.Exactly(2));
            _unitOfWork.Verify(u => u.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        });
    }
}
