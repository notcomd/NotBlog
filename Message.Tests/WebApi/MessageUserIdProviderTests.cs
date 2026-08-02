using Message.Web.API.Hubs;
using System.Security.Claims;

namespace Message.Tests.WebApi;

/// <summary>
/// IUserIdProvider（F-04）单元测试：验证从 JWT Claim（sub / NameIdentifier / user_guid）解析用户 ID 的优先级。
/// </summary>
[TestFixture]
public class MessageUserIdProviderTests
{
    [Test]
    public void ResolveUserId_无Claim时应返回Null()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.That(MessageUserIdProvider.ResolveUserId(principal), Is.Null);
    }

    [Test]
    public void ResolveUserId_有subClaim时应优先返回sub()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "11111111-1111-1111-1111-111111111111"),
            new Claim(ClaimTypes.NameIdentifier, "22222222-2222-2222-2222-222222222222"),
            new Claim("user_guid", "33333333-3333-3333-3333-333333333333")
        }));

        Assert.That(MessageUserIdProvider.ResolveUserId(principal),
            Is.EqualTo("11111111-1111-1111-1111-111111111111"));
    }

    [Test]
    public void ResolveUserId_无sub时应回退到NameIdentifier()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "22222222-2222-2222-2222-222222222222")
        }));

        Assert.That(MessageUserIdProvider.ResolveUserId(principal),
            Is.EqualTo("22222222-2222-2222-2222-222222222222"));
    }

    [Test]
    public void ResolveUserId_无sub和NameIdentifier时应回退到user_guid()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("user_guid", "33333333-3333-3333-3333-333333333333")
        }));

        Assert.That(MessageUserIdProvider.ResolveUserId(principal),
            Is.EqualTo("33333333-3333-3333-3333-333333333333"));
    }
}
