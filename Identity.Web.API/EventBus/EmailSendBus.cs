using Notcomd.NotEmail.Core;

namespace Identity.Web.API.EventBus;

[EvenBusName("Identity.User.Code")]
public class EmailSendBus(IEmailSender email, IEmailCodeSend emailCodeSend, ILogger<IEmailCodeSend> logger)
    : JsonIntegrationEventHandler<EmailSendRecord>
{
    private readonly IEmailSender _email = email ?? throw new ArgumentNullException(nameof(email));

    private readonly IEmailCodeSend _emailCodeSend =
        emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));

    private readonly ILogger<IEmailCodeSend> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    public async Task Handle(EmailSendRecord notification, CancellationToken cancellationToken)
    {
        await _emailCodeSend.SendEmailCodeAsync(notification.ToEmail, notification.Code.ToString());
        _logger.LogInformation($"date:{DateTime.UtcNow},邮件发送{notification.ToEmail}");
    }

    protected override Task EventDlerJson(string eventName, EmailSendRecord? eventData)
    {
        return Handle(eventData, CancellationToken.None);
    }
}