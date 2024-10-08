using EmailSendServer;
using Identity.Domain.IRepository;
using MediatR;

namespace Identity.Web.API.EventBus;

public class EmailSendBus: INotificationHandler<EmailSendRecord>
{
    public EmailSendBus(IEmail email, IEmailCodeSend emailCodeSend, ILogger<IEmailCodeSend> logger)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        _emailCodeSend = emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IEmail _email;
    private IEmailCodeSend _emailCodeSend;
    private ILogger<IEmailCodeSend> _logger;
    
    
    
    public Task Handle(EmailSendRecord notification, CancellationToken cancellationToken)
    {
        
        return Task.CompletedTask;
    }
}