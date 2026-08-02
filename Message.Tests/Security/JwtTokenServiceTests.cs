using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Notcomd.Token.JWT;
using Notcomd.Token.JWT.Core;
using NUnit.Framework;
using SecurityAlgorithms = Notcomd.Token.JWT.Core.SecurityAlgorithms;

namespace Message.Tests.Security;

/// <summary>
/// S-12 刷新 Token 逻辑单元测试。
/// 覆盖：刷新成功返回新 Token 对、RefreshToken 单次使用（二次使用被拒）、
/// 无效格式/篡改签名/过期被拒。
/// </summary>
[TestFixture]
public class JwtTokenServiceTests
{
    /// <summary>48 字节密钥（HS384 最低要求）</summary>
    private const string TestKey = "test-secret-key-0123456789abcdef0123456789abcdef";

    private JwtTokenService _service = null!;
    private JwtOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new JwtOptions
        {
            Issuer = "test-issuer",
            Audiences = "test-audience",
            PrivateKey = TestKey,
            ExpireSeconds = 7200,
            RefreshTokenExpireSeconds = 604800,
            Algorithm = SecurityAlgorithms.HmacSha384,
            ClockSkewSeconds = 5
        };
        _service = CreateService(_options);
    }

    private static JwtTokenService CreateService(JwtOptions options)
    {
        var snapshot = new Mock<IOptionsSnapshot<JwtOptions>>();
        snapshot.Setup(o => o.Value).Returns(options);
        return new JwtTokenService(snapshot.Object);
    }

    private static Claim[] BuildClaims() =>
        [new(ClaimTypes.NameIdentifier, "user-1"), new(ClaimTypes.Role, "Root")];

    [Test]
    public async Task RefreshToken_有效刷新_应返回新Token对()
    {
        var original = await _service.BuildTokenAsync(BuildClaims(), _options);

        var result = await _service.RefreshTokenAsync(original.RefreshToken!, _options);

        Assert.Multiple(() =>
        {
            Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
            Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public async Task RefreshToken_单次使用_重复使用旧Token应抛出SecurityTokenException()
    {
        var original = await _service.BuildTokenAsync(BuildClaims(), _options);
        await _service.RefreshTokenAsync(original.RefreshToken!, _options);

        Assert.ThrowsAsync<SecurityTokenException>(() =>
            _service.RefreshTokenAsync(original.RefreshToken!, _options));
    }

    [Test]
    public void RefreshToken_为空或空白_应抛出SecurityTokenException()
    {
        Assert.ThrowsAsync<SecurityTokenException>(() =>
            _service.RefreshTokenAsync("", _options));
        Assert.ThrowsAsync<SecurityTokenException>(() =>
            _service.RefreshTokenAsync("   ", _options));
    }

    [Test]
    public async Task RefreshToken_篡改签名_应抛出SecurityTokenException()
    {
        var original = await _service.BuildTokenAsync(BuildClaims(), _options);
        var parts = original.RefreshToken!.Split('.');
        var tampered = $"{parts[0]}.{FlipLastChar(parts[1])}";

        Assert.ThrowsAsync<SecurityTokenException>(() =>
            _service.RefreshTokenAsync(tampered, _options));
    }

    [Test]
    public async Task RefreshToken_已过期_应抛出SecurityTokenException()
    {
        // RefreshTokenExpireSeconds = -1：生成即已过期的 RefreshToken（无需等待）
        var shortOptions = new JwtOptions
        {
            Issuer = _options.Issuer,
            Audiences = _options.Audiences,
            PrivateKey = _options.PrivateKey,
            ExpireSeconds = 7200,
            RefreshTokenExpireSeconds = -1,
            Algorithm = SecurityAlgorithms.HmacSha384,
            ClockSkewSeconds = 5
        };
        var shortService = CreateService(shortOptions);
        var original = await shortService.BuildTokenAsync(BuildClaims(), shortOptions);

        Assert.ThrowsAsync<SecurityTokenException>(() =>
            shortService.RefreshTokenAsync(original.RefreshToken!, shortOptions));
    }

    private static string FlipLastChar(string input)
        => input[..^1] + (input[^1] == 'A' ? 'B' : 'A');
}
