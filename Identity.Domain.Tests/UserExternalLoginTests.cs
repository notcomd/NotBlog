using Identity.Domain.Entities.UserExternalLoginAggregate;

namespace Identity.Domain.Tests;

public class UserExternalLoginCreateTests
{
    [Theory]
    [InlineData(LoginProviderType.Google)]
    [InlineData(LoginProviderType.Microsoft)]
    [InlineData(LoginProviderType.GitHub)]
    [InlineData(LoginProviderType.WeChat)]
    [InlineData(LoginProviderType.QQ)]
    public void Create_AllProviders_ShouldSucceed(LoginProviderType provider)
    {
        var login = UserExternalLogin.Create(provider, "key-123", "Test User");

        Assert.Equal(provider, login.Provider);
        Assert.Equal("key-123", login.ProviderKey);
        Assert.Equal("Test User", login.ProviderDisplayName);
        Assert.NotEqual(Guid.Empty, login.LoginId);
        Assert.True(login.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(login.LastUsedAt <= DateTimeOffset.UtcNow);
        Assert.Null(login.ProviderUnionId);
        Assert.Equal(Guid.Empty, login.UserId); // 未绑定
    }

    [Fact]
    public void Create_WithUnionId_ShouldStore()
    {
        var login = UserExternalLogin.Create(
            LoginProviderType.WeChat, "openid-abc", "WeChatUser", unionId: "union-xyz");

        Assert.Equal("openid-abc", login.ProviderKey);
        Assert.Equal("union-xyz", login.ProviderUnionId);
        Assert.Equal("WeChatUser", login.ProviderDisplayName);
    }

    [Fact]
    public void Create_WithUnionIdNull_ShouldBeNull()
    {
        var login = UserExternalLogin.Create(
            LoginProviderType.Google, "g-sub", "GoogleUser", unionId: null);

        Assert.Null(login.ProviderUnionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ProviderKeyNullOrEmpty_ShouldThrow(string? invalidKey)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            UserExternalLogin.Create(LoginProviderType.Google, invalidKey!, "User"));

        Assert.Equal("providerKey", ex.ParamName);
    }

    [Fact]
    public void Create_DisplayNameNull_ShouldThrow()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            UserExternalLogin.Create(LoginProviderType.GitHub, "key", null!));

        Assert.Equal("displayName", ex.ParamName);
    }

    [Fact]
    public void Create_LoginId_ShouldBeGuidV7()
    {
        var login = UserExternalLogin.Create(LoginProviderType.QQ, "qq-key", "QQUser");

        Assert.NotEqual(Guid.Empty, login.LoginId);
        Assert.Equal(7, login.LoginId.Version); // Guid v7
    }
}

public class UserExternalLoginBindingTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void LinkUser_ValidUserId_ShouldBind()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "g-id", "User");

        login.LinkUser(_userId);

        Assert.Equal(_userId, login.UserId);
    }

    [Fact]
    public void LinkUser_AlreadyBound_ShouldThrow()
    {
        var login = UserExternalLogin.Create(LoginProviderType.GitHub, "gh-id", "User");
        login.LinkUser(_userId);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            login.LinkUser(Guid.NewGuid()));

        Assert.Contains("已绑定用户", ex.Message);
        Assert.Contains(_userId.ToString(), ex.Message);
    }

    [Fact]
    public void LinkUser_EmptyGuid_ShouldThrow()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Microsoft, "ms-id", "User");

        var ex = Assert.Throws<ArgumentException>(() =>
            login.LinkUser(Guid.Empty));

        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void UnlinkUser_ShouldClearUserId()
    {
        var login = UserExternalLogin.Create(LoginProviderType.WeChat, "wx-openid", "WxUser");
        login.LinkUser(_userId);
        Assert.Equal(_userId, login.UserId);

        login.UnlinkUser();

        Assert.Equal(Guid.Empty, login.UserId);
    }

    [Fact]
    public void UnlinkThenRelink_DifferentUser_ShouldSucceed()
    {
        var login = UserExternalLogin.Create(LoginProviderType.QQ, "qq-id", "QQUser");
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        login.LinkUser(userA);
        login.UnlinkUser();
        login.LinkUser(userB);

        Assert.Equal(userB, login.UserId);
    }

    [Fact]
    public void UnlinkUser_NotBound_ShouldNotThrow()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "g-2", "User");

        // 未绑定时解绑应无副作用
        login.UnlinkUser();

        Assert.Equal(Guid.Empty, login.UserId);
    }

    [Fact]
    public void LinkUser_SameUserTwiceAfterUnlink_ShouldSucceed()
    {
        var login = UserExternalLogin.Create(LoginProviderType.GitHub, "gh-2", "User");
        login.LinkUser(_userId);
        login.UnlinkUser();
        login.LinkUser(_userId);

        Assert.Equal(_userId, login.UserId);
    }
}

public class UserExternalLoginTokenTests
{
    private readonly DateTimeOffset _expiresAt = DateTimeOffset.UtcNow.AddHours(1);

    [Fact]
    public void UpdateTokens_Valid_ShouldStoreAll()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "key", "User");
        var beforeUpdate = login.LastUsedAt;

        // 模拟加密后的 Token（实际加密在上层服务完成）
        var encryptedAccess = "AES256:IV+CT==:base64encryptedbody";
        var encryptedRefresh = "AES256:IV+CT==:base64encryptedRefresh";

        login.UpdateTokens(encryptedAccess, encryptedRefresh, _expiresAt);

        Assert.Equal(encryptedAccess, login.EncryptedAccessToken);
        Assert.Equal(encryptedRefresh, login.EncryptedRefreshToken);
        Assert.Equal(_expiresAt, login.TokenExpiresAt);
        Assert.True(login.LastUsedAt >= beforeUpdate);
    }

    [Fact]
    public void UpdateTokens_RefreshTokenNull_ShouldClear()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Microsoft, "ms-key", "User");

        // 先设置 refresh token，再清空
        login.UpdateTokens("enc-access", "enc-refresh", _expiresAt);
        login.UpdateTokens("enc-access-2", null, _expiresAt);

        Assert.Equal("enc-access-2", login.EncryptedAccessToken);
        Assert.Null(login.EncryptedRefreshToken);
    }

    [Fact]
    public void UpdateTokens_ExpiresAtNull_ShouldStoreNull()
    {
        var login = UserExternalLogin.Create(LoginProviderType.GitHub, "gh-key", "User");

        login.UpdateTokens("enc-token", null, null);

        Assert.Equal("enc-token", login.EncryptedAccessToken);
        Assert.Null(login.TokenExpiresAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTokens_AccessTokenNullOrEmpty_ShouldThrow(string? invalidToken)
    {
        var login = UserExternalLogin.Create(LoginProviderType.QQ, "qq-key", "User");

        var ex = Assert.Throws<ArgumentException>(() =>
            login.UpdateTokens(invalidToken!, null, _expiresAt));

        Assert.Equal("encryptedAccessToken", ex.ParamName);
    }

    [Fact]
    public void UpdateTokens_ShouldUpdateLastUsedAt()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "g-3", "User");
        var originalLastUsed = login.LastUsedAt;

        // 等待片刻
        Thread.Sleep(10);
        login.UpdateTokens("enc-new-token", null, _expiresAt);

        Assert.True(login.LastUsedAt > originalLastUsed);
    }

    [Fact]
    public void UpdateTokens_MultipleUpdates_ShouldOverwrite()
    {
        var login = UserExternalLogin.Create(LoginProviderType.WeChat, "wx-2", "WxUser");

        login.UpdateTokens("token-v1", "refresh-v1", DateTimeOffset.UtcNow.AddMinutes(30));
        login.UpdateTokens("token-v2", "refresh-v2", DateTimeOffset.UtcNow.AddHours(2));

        Assert.Equal("token-v2", login.EncryptedAccessToken);
        Assert.Equal("refresh-v2", login.EncryptedRefreshToken);
        Assert.True(login.TokenExpiresAt > DateTimeOffset.UtcNow.AddHours(1));
    }
}

public class UserExternalLoginMarkAsUsedTests
{
    [Fact]
    public void MarkAsUsed_ShouldUpdateLastUsedAt()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "g-key", "User");
        var original = login.LastUsedAt;

        Thread.Sleep(10);
        login.MarkAsUsed();

        Assert.True(login.LastUsedAt > original);
    }

    [Fact]
    public void MarkAsUsed_MultipleCalls_ShouldKeepUpdating()
    {
        var login = UserExternalLogin.Create(LoginProviderType.GitHub, "gh-key", "User");

        var t1 = login.LastUsedAt;
        Thread.Sleep(5);
        login.MarkAsUsed();
        var t2 = login.LastUsedAt;
        Thread.Sleep(5);
        login.MarkAsUsed();
        var t3 = login.LastUsedAt;

        Assert.True(t2 > t1);
        Assert.True(t3 > t2);
    }

    [Fact]
    public void MarkAsUsed_OnUnboundLogin_ShouldWork()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Microsoft, "ms-2", "User");

        login.MarkAsUsed();

        Assert.Equal(Guid.Empty, login.UserId);
        Assert.True(login.LastUsedAt > login.CreatedAt);
    }
}

public class UserExternalLoginPropertyTests
{
    [Fact]
    public void NewInstance_ShouldHaveDefaultValues()
    {
        var login = UserExternalLogin.Create(LoginProviderType.Google, "key", "User");

        Assert.Equal(Guid.Empty, login.UserId);
        Assert.Null(login.EncryptedAccessToken);
        Assert.Null(login.EncryptedRefreshToken);
        Assert.Null(login.TokenExpiresAt);
        Assert.Null(login.ProviderUnionId);
    }

    [Fact]
    public void CreatedAt_ShouldBeUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddMilliseconds(-100);
        var login = UserExternalLogin.Create(LoginProviderType.GitHub, "key", "User");

        Assert.True(login.CreatedAt >= before);
        Assert.True(login.CreatedAt <= DateTimeOffset.UtcNow.AddMilliseconds(100));
    }

    [Fact]
    public void TwoInstances_ShouldHaveDifferentLoginIds()
    {
        var login1 = UserExternalLogin.Create(LoginProviderType.Google, "key-A", "UserA");
        var login2 = UserExternalLogin.Create(LoginProviderType.Google, "key-B", "UserB");

        Assert.NotEqual(login1.LoginId, login2.LoginId);
    }
}
