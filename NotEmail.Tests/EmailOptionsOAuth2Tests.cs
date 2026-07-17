﻿﻿﻿using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class EmailOptionsOAuth2Tests
{
    [Fact]
    public void IsOAuth2Configured_WithUseOAuth2AndCallback_ShouldReturnTrue()
    {
        var options = new EmailOptions
        {
            UseOAuth2 = true,
            AccessTokenCallback = _ => Task.FromResult("token-abc")
        };

        Assert.True(options.IsOAuth2Configured);
    }

    [Fact]
    public void IsOAuth2Configured_WithoutCallback_ShouldReturnFalse()
    {
        var options = new EmailOptions { UseOAuth2 = true };
        Assert.False(options.IsOAuth2Configured);
    }

    [Fact]
    public void IsOAuth2Configured_WithoutUseOAuth2_ShouldReturnFalse()
    {
        var options = new EmailOptions
        {
            UseOAuth2 = false,
            AccessTokenCallback = _ => Task.FromResult("token-abc")
        };
        Assert.False(options.IsOAuth2Configured);
    }

    [Fact]
    public void IsPasswordConfigured_WithPassword_ShouldReturnTrue()
    {
        var options = new EmailOptions { Password = "secret" };
        Assert.True(options.IsPasswordConfigured);
    }

    [Fact]
    public void IsPasswordConfigured_WithoutPassword_ShouldReturnFalse()
    {
        var options = new EmailOptions { Password = null };
        Assert.False(options.IsPasswordConfigured);
    }

    [Fact]
    public void IsPasswordConfigured_EmptyString_ShouldReturnFalse()
    {
        var options = new EmailOptions { Password = "" };
        Assert.False(options.IsPasswordConfigured);
    }

    [Fact]
    public void OAuthScopes_ShouldDefaultToEmpty()
    {
        var options = new EmailOptions();
        Assert.Empty(options.OAuthScopes);
    }

    [Fact]
    public void TenantId_ShouldDefaultToNull()
    {
        var options = new EmailOptions();
        Assert.Null(options.TenantId);
    }

    [Fact]
    public void DefaultPassword_ShouldAllowNull()
    {
        var options = new EmailOptions();
        Assert.Null(options.Password);
    }

    [Fact]
    public void BothAuthModesConfigured_OAuth2Wins()
    {
        var options = new EmailOptions
        {
            Password = "password",
            UseOAuth2 = true,
            AccessTokenCallback = _ => Task.FromResult("token")
        };

        Assert.True(options.IsOAuth2Configured);
        Assert.True(options.IsPasswordConfigured);
        // OAuth2 优先，两个都可配
    }

    [Fact]
    public async Task OAuthTokenCallback_ShouldBeInvocable()
    {
        var called = false;
        var options = new EmailOptions
        {
            UseOAuth2 = true,
            AccessTokenCallback = _ =>
            {
                called = true;
                return Task.FromResult("mock-token");
            }
        };

        var token = await options.AccessTokenCallback!(CancellationToken.None);
        Assert.Equal("mock-token", token);
        Assert.True(called);
    }
}
