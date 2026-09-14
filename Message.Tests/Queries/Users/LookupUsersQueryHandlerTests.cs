using Message.Domain.Entities.User;
using Message.Domain.IRepository;
using Message.Web.API.Application.Queries.Users;
using Moq;

namespace Message.Tests.Queries.Users;

/// <summary>
/// 查找用户查询处理程序单元测试。
/// 覆盖：邮箱（含 @）精确取单条、昵称精确匹配多条、未命中返回空集合、昵称缺失回退空串。
/// </summary>
[TestFixture]
public class LookupUsersQueryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherId = Guid.NewGuid();

    private Mock<IUserInfoRepository> _userInfoRepository = null!;
    private LookupUsersQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _userInfoRepository = new Mock<IUserInfoRepository>();
        _handler = new LookupUsersQueryHandler(_userInfoRepository.Object);
    }

    [Test]
    public async Task Handler_关键词含邮箱_应按邮箱精确查找并返回单条()
    {
        _userInfoRepository.Setup(r => r.GetByEmailAsync("look@test.com"))
            .ReturnsAsync(UserInfo.Create(UserId, "look@test.com", "小明", new Uri("https://cdn.test/avatar.png")));

        var result = (await _handler.Handler(new LookupUsersQuery("look@test.com", 5), CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserGuid, Is.EqualTo(UserId));
            Assert.That(result[0].UserName, Is.EqualTo("小明"));
            Assert.That(result[0].Avatar, Is.EqualTo("https://cdn.test/avatar.png"));
            // 邮箱路径不得回退到昵称查询
            _userInfoRepository.Verify(r => r.GetByNickNameAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        });
    }

    [Test]
    public async Task Handler_邮箱未命中_应返回空集合()
    {
        _userInfoRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((UserInfo?)null);

        var result = await _handler.Handler(new LookupUsersQuery("none@test.com", 5), CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Handler_关键词为昵称_应按昵称精确查找并沿用条数上限()
    {
        _userInfoRepository.Setup(r => r.GetByNickNameAsync("小明", 5))
            .ReturnsAsync(new[]
            {
                UserInfo.Create(UserId, "a@test.com", "小明"),
                UserInfo.Create(OtherId, "b@test.com", "小明")
            });

        var result = (await _handler.Handler(new LookupUsersQuery("小明", 5), CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(u => u.UserGuid), Is.EquivalentTo(new[] { UserId, OtherId }));
            // 昵称路径不得触发邮箱查询
            _userInfoRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        });
    }

    [Test]
    public async Task Handler_关键词未命中昵称_应返回空集合()
    {
        _userInfoRepository.Setup(r => r.GetByNickNameAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<UserInfo>());

        var result = await _handler.Handler(new LookupUsersQuery("查无此人", 5), CancellationToken.None);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Handler_用户未设置昵称_应回退为空字符串()
    {
        _userInfoRepository.Setup(r => r.GetByEmailAsync("nonick@test.com"))
            .ReturnsAsync(UserInfo.Create(UserId, "nonick@test.com"));

        var result = (await _handler.Handler(new LookupUsersQuery("nonick@test.com", 5), CancellationToken.None)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result[0].UserName, Is.EqualTo(string.Empty));
            Assert.That(result[0].Avatar, Is.Null);
        });
    }
}
