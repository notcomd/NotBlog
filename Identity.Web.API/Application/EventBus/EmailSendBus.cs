namespace Identity.Web.API.Application.EventBus;

[EvenBusName("Identity.User.Code")]
public class EmailSendBus : JsonIntegrationEventHandler<EmailSendRecord>
{

    private readonly IEmail _email;
    private readonly IEmailCodeSend _emailCodeSend;
    private readonly ILogger<IEmailCodeSend> _logger;
    public EmailSendBus(IEmail email, IEmailCodeSend emailCodeSend, ILogger<IEmailCodeSend> logger)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _emailCodeSend = emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task Handle(EmailSendRecord notification, CancellationToken cancellationToken)
    {
        await _emailCodeSend.SendEmailCodeAsync(notification.ToEmail, notification.Code.ToString());
        _logger.LogInformation($"date:{DateTime.UtcNow},邮件发送{notification.ToEmail}");

    }

    public async override Task EventDlerJson(string eventName, EmailSendRecord? eventData)
    {
        await _emailCodeSend.SendEmailCodeAsync(eventData?.ToEmail, eventData?.Code.ToString());
        _logger.LogInformation($"date:{DateTime.UtcNow},邮件发送{eventData.ToEmail}");
    }
}