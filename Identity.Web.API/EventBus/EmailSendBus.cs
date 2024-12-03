using EmailSendServer;
using Identity.Domain.IRepository;
using MediatR;

namespace Identity.Web.API.EventBus;

public class EmailSendBus : INotificationHandler<EmailSendRecord>
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


    public Task Handle(EmailSendRecord notification, CancellationToken cancellationToken)
    {
        _emailCodeSend.SendEmailCodeAsync(notification.ToEmail, notification.Code.ToString());
        _logger.LogInformation($"date:{DateTime.UtcNow},邮件发送{notification.ToEmail}");
        return Task.CompletedTask;
    }
}