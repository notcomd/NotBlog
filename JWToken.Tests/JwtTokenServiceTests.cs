﻿using System.Security.Claims;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace Notcomd.Token.JWT.Tests;

public class JwtOptionsTests
{
    [Fact]
    public void Audiencs_BackwardCompatibility_ShouldWork()
    {
        var options = new JwtOptions
        {
            Issuer = "issuer",
            Audiencs = "audience-old",
            PrivateKey = "supersecretkey1234567890abcdefgh",
            ExpirSeconds = 3600
        };

        Assert.Equal("audience-old", options.Audiencs);
        Assert.Equal("audience-old", options.Audience);
    }

    [Fact]
    public void Audience_NewProperty_ShouldWork()
    {
        var options = new JwtOptions
        {
            Issuer = "issuer",
            Audience = "new-audience",
            PrivateKey = "key"
        };

        Assert.Equal("new-audience", options.Audience);
        Assert.Equal("new-audience", options.Audiencs);
    }

    [Fact]
    public void DefaultAlgorithm_ShouldBeHS256()
    {
        var options = new JwtOptions();
        Assert.Equal("HS256", options.Algorithm);
    }

    [Fact]
    public void DefaultExpirSeconds_ShouldBe3600()
    {
        var options = new JwtOptions();
        Assert.Equal(3600, options.ExpirSeconds);
    }

    [Fact]
    public void RefreshTokenExpirSeconds_DefaultShouldBe7Days()
    {
        var options = new JwtOptions();
        Assert.Equal(604800, options.RefreshTokenExpirSeconds);
    }

    [Fact]
    public void ClockSkewSeconds_DefaultShouldBe300()
    {
        var options = new JwtOptions();
        Assert.Equal(300, options.ClockSkewSeconds);
    }
}

public class JwtTokenServiceTests
{
    private readonly JwtOptions _jwtOptions;
    private readonly IJwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            Issuer = "test-issuer",
            Audiencs = "test-audience",
            PrivateKey = "this-is-a-256-bit-secret-key!!-=abcdefghijklmn",
            ExpirSeconds = 3600
        };

        var optionsSnapshot = new OptionsSnapshotWrapper(_jwtOptions);
        _service = new JwtTokenService(optionsSnapshot);
    }

    [Fact]
    public void BuilderTokenAsync_ShouldGenerateValidToken()
    {
        var claims = new[] { new Claim("sub", "user-123"), new Claim("role", "admin") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        // JWT 格式: header.payload.signature
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public async Task BuildTokenAsync_ShouldReturnTokenResult()
    {
        var claims = new[] { new Claim("sub", "user-456") };
        var result = await _service.BuildTokenAsync(claims, _jwtOptions);

        Assert.NotNull(result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task JwtSecurityTokenHandlerAsync_ShouldValidateToken()
    {
        var claims = new[] { new Claim("sub", "validate-test") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        var validation = await _service.JwtSecurityTokenHandlerAsync(
            _jwtOptions.PrivateKey, token);

        Assert.True(validation.IsValid);
    }

    [Fact]
    public async Task JwtSecurityTokenHandlerAsync_WithWrongKey_ShouldFail()
    {
        var claims = new[] { new Claim("sub", "wrong-key-test") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        var validation = await _service.JwtSecurityTokenHandlerAsync(
            "wrong-wrong-wrong-key-here!!!!!!!", token);

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task JwtSecurityTokenHandlerAsync_TamperedToken_ShouldFail()
    {
        var claims = new[] { new Claim("sub", "tampered") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        // 修改 token（最后一个字符翻转）
        var tamperedToken = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var validation = await _service.JwtSecurityTokenHandlerAsync(
            _jwtOptions.PrivateKey, tamperedToken);

        Assert.False(validation.IsValid);
    }

    [Fact]
    public void ValidateToken_ShouldReturnClaimsPrincipal()
    {
        var claims = new[] { new Claim("sub", "user-789"), new Claim("role", "user") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        var principal = _service.ValidateToken(token, _jwtOptions);

        Assert.NotNull(principal);
        // JWT "sub" claim 会被 JwtSecurityTokenHandler 映射为 ClaimTypes.NameIdentifier
        var sub = principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        Assert.Equal("user-789", sub);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ShouldReturnNull()
    {
        var principal = _service.ValidateToken("invalid.token.here", _jwtOptions);
        Assert.Null(principal);
    }

    [Fact]
    public async Task RevokeToken_ShouldBlacklist()
    {
        var claims = new[] { new Claim("sub", "revoke-me") };
        var token = _service.BuilderTokenAsync(claims, _jwtOptions);

        await _service.RevokeTokenAsync(token);

        var principal = _service.ValidateToken(token, _jwtOptions);
        Assert.Null(principal);
    }
}

/// <summary>
/// IOptionsSnapshot 测试适配器
/// </summary>
internal class OptionsSnapshotWrapper : IOptionsSnapshot<JwtOptions>
{
    public OptionsSnapshotWrapper(JwtOptions value) => Value = value;
    public JwtOptions Value { get; }
    public JwtOptions Get(string? name) => Value;
}
