﻿﻿﻿﻿using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class EmailMessageTests
{
    [Fact]
    public void Constructor_ShouldSetRequiredFields()
    {
        var message = new EmailMessage("user@example.com", "Subject", "<p>Body</p>");

        Assert.Equal("user@example.com", message.To);
        Assert.Equal("Subject", message.Subject);
        Assert.Equal("<p>Body</p>", message.HtmlBody);
        Assert.Null(message.PlainTextBody);
    }

    [Fact]
    public void Constructor_PlainText_ShouldNotSetHtml()
    {
        var message = new EmailMessage("user@example.com", "Subject", "Plain Body", isHtml: false);

        Assert.Null(message.HtmlBody);
        Assert.Equal("Plain Body", message.PlainTextBody);
    }

    [Fact]
    public void Cc_ShouldBeEmptyByDefault()
    {
        var message = new EmailMessage("user@example.com", "Subject", "Body");

        Assert.Empty(message.Cc);
    }

    [Fact]
    public void Attachments_ShouldBeEmptyByDefault()
    {
        var message = new EmailMessage("user@example.com", "Subject", "Body");

        Assert.Empty(message.Attachments);
    }

    [Fact]
    public void Priority_ShouldDefaultToNormal()
    {
        var message = new EmailMessage("user@example.com", "Subject", "Body");

        Assert.Equal(EmailPriority.Normal, message.Priority);
    }

    [Fact]
    public void Constructor_NullTo_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EmailMessage(null!));
    }

    [Fact]
    public void Constructor_OnlyTo_ShouldHaveEmptySubject()
    {
        var message = new EmailMessage("user@example.com");

        Assert.Equal("", message.Subject);
        Assert.Null(message.HtmlBody);
    }
}
