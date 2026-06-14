using Moq;
using Notcomd.NotEmail;

namespace Notcomd.NotEmail.Tests;

public class EmailManagerTests
{
    private readonly Mock<IEmailSender> _senderMock;
    private readonly EmailManager _manager;

    public EmailManagerTests()
    {
        _senderMock = new Mock<IEmailSender>();
        _manager = new EmailManager(_senderMock.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldDelegateToSender()
    {
        var message = new EmailMessage("to@test.com", "Test", "Body");
        _senderMock.Setup(s => s.SendAsync(message, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendResult.Ok());

        var result = await _manager.SendAsync(message);

        Assert.True(result.Success);
        _senderMock.Verify(s => s.SendAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInbox_WithoutReceiver_ShouldThrow()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.GetInboxAsync());
    }

    [Fact]
    public async Task Delete_WithoutReceiver_ShouldThrow()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.DeleteAsync(1));
    }

    [Fact]
    public async Task SendFromTemplate_WithoutTemplateService_ShouldThrow()
    {
        var message = new EmailMessage("to@test.com");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _manager.SendFromTemplateAsync("Any", new { }, "to@test.com"));
    }
}
