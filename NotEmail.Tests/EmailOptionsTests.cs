﻿﻿﻿using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class EmailOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeGmail()
    {
        var options = new EmailOptions();

        Assert.Equal("smtp.gmail.com", options.SmtpHost);
        Assert.Equal(587, options.SmtpPort);
        Assert.Equal("imap.gmail.com", options.ImapHost);
        Assert.Equal(993, options.ImapPort);
        Assert.True(options.UseSsl);
        Assert.Equal(3, options.MaxRetryCount);
        Assert.Equal(1000, options.RetryIntervalMs);
        Assert.Equal(30000, options.SendTimeoutMs);
    }

    [Fact]
    public void SendResult_Ok_ShouldReturnSuccess()
    {
        var result = SendResult.Ok("msg-123");

        Assert.True(result.Success);
        Assert.Equal("msg-123", result.MessageId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void SendResult_Fail_ShouldReturnError()
    {
        var result = SendResult.Fail("SMTP connection refused");

        Assert.False(result.Success);
        Assert.Equal("SMTP connection refused", result.ErrorMessage);
        Assert.Null(result.MessageId);
    }

    [Fact]
    public void EmailAttachment_ShouldStoreAllProperties()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("file content");
        var attachment = new EmailAttachment("report.pdf", data, "application/pdf");

        Assert.Equal("report.pdf", attachment.FileName);
        Assert.Equal(data, attachment.Data);
        Assert.Equal("application/pdf", attachment.MimeType);
    }

    [Fact]
    public void EmailAttachment_NullFileName_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmailAttachment(null!, new byte[1]));
    }

    [Fact]
    public void EmailAttachment_NullData_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmailAttachment("test.txt", null!));
    }
}
