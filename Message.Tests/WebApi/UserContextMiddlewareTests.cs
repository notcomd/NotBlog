using System.Security.Claims;
using Message.Domain.IServices;
using Message.Web.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Message.Tests.WebApi;

/// <summary>
/// 用户上下文中间件安全测试（修复 S-02）。
/// 核心断言：身份<b>仅</b>来自已验证 JWT 的 Claim（sub / NameIdentifier / user_guid），
/// 伪造 X-User-Id / X-User-Roles 请求头无法生效；角色仅从 JWT Role Claim 解析。
/// </summary>
[TestFixture]
public class UserContextMiddlewareTests
{
    private static readonly Guid JwtUserId = Guid.NewGuid();
    private static readonly Guid ForgedUserId = Guid.NewGuid();

    private Mock<ICurrentUserService> _currentUser = null!;
    private UserContextMiddleware _middleware = null!;

    [SetUp]
    public void Setup()
    {
        _currentUser = new Mock<ICurrentUserService>();
        _middleware = new UserContextMiddleware(
            _ => Task.CompletedTask,
            new Mock<ILogger<UserContextMiddleware>>().Object);
    }

    // ---------- 负向用例：未认证时身份不得生效 ----------

    [Test]
    public async Task 未认证_携带伪造X_User_Id头_不应注入任何身份()
    {
        var context = CreateContext(
            user: new ClaimsPrincipal(new ClaimsIdentity()), // 未认证
            xUserId: ForgedUserId.ToString());

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never,
            "未认证请求即使携带 X-User-Id 头也不得注入身份（防直连伪造）");
    }

    [Test]
    public async Task 未认证_携带伪造X_User_Roles头_不得获得角色()
    {
        var context = CreateContext(
            user: new ClaimsPrincipal(new ClaimsIdentity()),
            xUserId: ForgedUserId.ToString(),
            xRoles: "Admin,Root");

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never,
            "未认证请求的 X-User-Roles 头不得产生任何角色");
    }

    [Test]
    public async Task 未认证_无任何头_不调用SetUser()
    {
        var context = CreateContext(user: new ClaimsPrincipal(new ClaimsIdentity()));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never);
    }

    // ---------- 正向用例：JWT 身份生效 ----------

    [Test]
    public async Task 已认证_应使用subClaim注入身份()
    {
        var context = CreateContext(user: Authenticated(new Claim("sub", JwtUserId.ToString())));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId, It.IsAny<string[]>()), Times.Once);
    }

    [Test]
    public async Task 已认证_应兼容NameIdentifierClaim()
    {
        var context = CreateContext(
            user: Authenticated(new Claim(ClaimTypes.NameIdentifier, JwtUserId.ToString())));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId, It.IsAny<string[]>()), Times.Once);
    }

    [Test]
    public async Task 已认证_应兼容user_guidClaim()
    {
        var context = CreateContext(user: Authenticated(new Claim("user_guid", JwtUserId.ToString())));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId, It.IsAny<string[]>()), Times.Once);
    }

    // ---------- 负向用例：伪造头与 JWT 不一致时忽略 ----------

    [Test]
    public async Task 已认证_伪造X_User_Id与JWT不一致_应忽略请求头以JWT为准()
    {
        var context = CreateContext(
            user: Authenticated(new Claim("sub", JwtUserId.ToString())),
            xUserId: ForgedUserId.ToString());

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId, It.IsAny<string[]>()), Times.Once);
        _currentUser.Verify(m => m.SetUser(ForgedUserId, It.IsAny<string[]>()), Times.Never,
            "X-User-Id 与 JWT 身份不一致时必须忽略请求头");
    }

    [Test]
    public async Task 已认证_伪造X_User_Roles_角色应取自JWTClaim而非请求头()
    {
        var context = CreateContext(
            user: Authenticated(new Claim("sub", JwtUserId.ToString()),
                new Claim(ClaimTypes.Role, "Admin,User")),
            xUserId: JwtUserId.ToString(),
            xRoles: "Root,SuperAdmin"); // 伪造管理员角色

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId,
            It.Is<string[]>(r => r.SequenceEqual(new[] { "Admin", "User" }))), Times.Once,
            "角色必须取自 JWT Role Claim（逗号拼接需拆分），伪造的 X-User-Roles 头不得生效");
    }

    [Test]
    public async Task 已认证_多个RoleClaim应合并去重()
    {
        var context = CreateContext(
            user: Authenticated(new Claim("sub", JwtUserId.ToString()),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Admin")));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(JwtUserId,
            It.Is<string[]>(r => r.Contains("Admin") && r.Contains("User"))), Times.Once);
    }

    [Test]
    public async Task 已认证_无userIdClaim_不调用SetUser()
    {
        var context = CreateContext(
            user: Authenticated(new Claim(ClaimTypes.Name, "someone")));

        await _middleware.InvokeAsync(context, _currentUser.Object);

        _currentUser.Verify(m => m.SetUser(It.IsAny<Guid>(), It.IsAny<string[]>()), Times.Never);
    }

    // ---------- 辅助 ----------

    private static DefaultHttpContext CreateContext(
        ClaimsPrincipal? user = null, string? xUserId = null, string? xRoles = null)
    {
        var context = new DefaultHttpContext();
        if (user != null)
            context.User = user;
        if (xUserId != null)
            context.Request.Headers["X-User-Id"] = xUserId;
        if (xRoles != null)
            context.Request.Headers["X-User-Roles"] = xRoles;
        return context;
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Bearer"));
}
