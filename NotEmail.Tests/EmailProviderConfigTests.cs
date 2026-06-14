using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class EmailProviderConfigTests
{
    [Fact]
    public void Google_ShouldSetCorrectSmtpAndImap()
    {
        var options = EmailProviderConfig.Google("test@gmail.com", "app-password");

        Assert.Equal("smtp.gmail.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("imap.gmail.com", options.ImapHost);
        Assert.Equal(993, options.ImapPort);
        Assert.Equal("test@gmail.com", options.FromEmail);
        Assert.Equal("app-password", options.Password);
        Assert.True(options.UseSsl);
        Assert.True(options.EnableImap);
    }

    [Fact]
    public void Outlook_ShouldSetCorrectSmtpAndImap()
    {
        var options = EmailProviderConfig.Outlook("test@outlook.com", "password");

        Assert.Equal("smtp-mail.outlook.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("outlook.office365.com", options.ImapHost);
    }

    [Fact]
    public void QQ_ShouldSetCorrectSmtpAndImap()
    {
        var options = EmailProviderConfig.QQ("123456@qq.com", "auth-code");

        Assert.Equal("smtp.qq.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("imap.qq.com", options.ImapHost);
    }

    [Fact]
    public void NetEase163_ShouldSetCorrectSmtp()
    {
        var options = EmailProviderConfig.NetEase163("test@163.com", "auth-code");

        Assert.Equal("smtp.163.com", options.SmtpHost);
        Assert.Equal(465, options.SmtpPort);
    }

    [Fact]
    public void NetEase126_ShouldSetCorrectSmtp()
    {
        var options = EmailProviderConfig.NetEase126("test@126.com", "auth-code");

        Assert.Equal("smtp.126.com", options.SmtpHost);
        Assert.Equal(465, options.SmtpPort);
    }

    [Fact]
    public void Aliyun_ShouldSetCorrectSmtp()
    {
        var options = EmailProviderConfig.Aliyun("admin@example.com", "password");

        Assert.Equal("smtp.mxhichina.com", options.SmtpHost);
        Assert.Equal(465, options.SmtpPort);
    }

    [Fact]
    public void Yahoo_ShouldSetCorrectSmtpAndImap()
    {
        var options = EmailProviderConfig.Yahoo("test@yahoo.com", "app-password");

        Assert.Equal("smtp.mail.yahoo.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("imap.mail.yahoo.com", options.ImapHost);
        Assert.Equal(993, options.ImapPort);
        Assert.True(options.UseSsl);
        Assert.True(options.EnableImap);
    }

    [Fact]
    public void Sohu_ShouldSetCorrectSmtpAndImap()
    {
        var options = EmailProviderConfig.Sohu("test@sohu.com", "auth-code");

        Assert.Equal("smtp.sohu.com", options.SmtpHost);
        Assert.Equal(465, options.SmtpPort);
        Assert.Equal("imap.sohu.com", options.ImapHost);
        Assert.Equal(993, options.ImapPort);
        Assert.True(options.UseSsl);
        Assert.True(options.EnableImap);
    }

    [Fact]
    public void FromProvider_ShouldReturnCorrectConfig()
    {
        var options = EmailProviderConfig.FromProvider(EmailProviderType.Google, "g@gmail.com", "pw");

        Assert.Equal("smtp.gmail.com", options.SmtpHost);
    }

    [Fact]
    public void FromProvider_Yahoo_ShouldReturnCorrectConfig()
    {
        var options = EmailProviderConfig.FromProvider(EmailProviderType.Yahoo, "y@yahoo.com", "pw");

        Assert.Equal("smtp.mail.yahoo.com", options.SmtpHost);
    }

    [Fact]
    public void FromProvider_Sohu_ShouldReturnCorrectConfig()
    {
        var options = EmailProviderConfig.FromProvider(EmailProviderType.Sohu, "s@sohu.com", "pw");

        Assert.Equal("smtp.sohu.com", options.SmtpHost);
    }

    [Fact]
    public void FromProvider_Custom_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(
            () => EmailProviderConfig.FromProvider(EmailProviderType.Custom, "a@b.com", "pw"));
    }

    // ── OAuth 2.0 测试 ──

    [Fact]
    public void GoogleOAuth2_ShouldConfigureCorrectly()
    {
        static Task<string> tokenCallback(CancellationToken ct) => Task.FromResult("google-token");

        var options = EmailProviderConfig.GoogleOAuth2("client-id", "client-secret", tokenCallback, "user@gmail.com");

        Assert.True(options.UseOAuth2);
        Assert.True(options.IsOAuth2Configured);
        Assert.Equal("client-id", options.ClientId);
        Assert.Equal("client-secret", options.ClientSecret);
        Assert.Equal("smtp.gmail.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("imap.gmail.com", options.ImapHost);
        Assert.Equal(993, options.ImapPort);
        Assert.Equal("user@gmail.com", options.FromEmail);
        Assert.Equal("https://accounts.google.com/o/oauth2/v2/auth", options.AuthorizationEndpoint);
        Assert.Equal("https://oauth2.googleapis.com/token", options.TokenEndpoint);
        Assert.Contains("https://mail.google.com/", options.OAuthScopes);
    }

    [Fact]
    public void OutlookOAuth2_ShouldConfigureCorrectly()
    {
        static Task<string> tokenCallback(CancellationToken ct) => Task.FromResult("ms-token");

        var options = EmailProviderConfig.OutlookOAuth2("app-id", "secret", tokenCallback, "consumers", "user@outlook.com");

        Assert.True(options.UseOAuth2);
        Assert.Equal("smtp-mail.outlook.com", options.SmtpHost);
        Assert.Equal("outlook.office365.com", options.ImapHost);
        Assert.Equal("consumers", options.TenantId);
        Assert.Contains("login.microsoftonline.com/consumers/oauth2/v2.0/authorize", options.AuthorizationEndpoint!);
        Assert.Contains("login.microsoftonline.com/consumers/oauth2/v2.0/token", options.TokenEndpoint!);
        Assert.Contains("https://outlook.office.com/SMTP.Send", options.OAuthScopes);
        Assert.Contains("https://outlook.office.com/IMAP.AccessAsUser.All", options.OAuthScopes);
        Assert.Contains("offline_access", options.OAuthScopes);
    }

    [Fact]
    public void OutlookOAuth2_DefaultTenant_ShouldBeCommon()
    {
        static Task<string> tokenCallback(CancellationToken ct) => Task.FromResult("token");

        var options = EmailProviderConfig.OutlookOAuth2("id", "secret", tokenCallback);

        Assert.Contains("login.microsoftonline.com/common/", options.AuthorizationEndpoint!);
    }

    [Fact]
    public void YahooOAuth2_ShouldConfigureCorrectly()
    {
        static Task<string> tokenCallback(CancellationToken ct) => Task.FromResult("yahoo-token");

        var options = EmailProviderConfig.YahooOAuth2("yahoo-id", "yahoo-secret", tokenCallback, "user@yahoo.com");

        Assert.True(options.UseOAuth2);
        Assert.Equal("smtp.mail.yahoo.com", options.SmtpHost);
        Assert.Equal("imap.mail.yahoo.com", options.ImapHost);
        Assert.Equal("https://api.login.yahoo.com/oauth2/request_auth", options.AuthorizationEndpoint);
        Assert.Equal("https://api.login.yahoo.com/oauth2/get_token", options.TokenEndpoint);
        Assert.Contains("mail-w", options.OAuthScopes);
    }

    [Fact]
    public void AllOAuth2Configs_ShouldHaveEnableImap()
    {
        static Task<string> cb(CancellationToken ct) => Task.FromResult("t");

        Assert.True(EmailProviderConfig.GoogleOAuth2("id", "secret", cb).EnableImap);
        Assert.True(EmailProviderConfig.OutlookOAuth2("id", "secret", cb).EnableImap);
        Assert.True(EmailProviderConfig.YahooOAuth2("id", "secret", cb).EnableImap);
    }
}
