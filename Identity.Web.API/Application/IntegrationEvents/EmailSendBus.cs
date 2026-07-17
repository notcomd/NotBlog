using Evenbus.Core;
using Notcomd.Evenbus;

namespace Identity.Web.API.Application.IntegrationEvents;

    [EvenBusName("Identity.User.Code")]
    public class EmailSendBus(IEmailSender email, IEmailCodeSend emailCodeSend, ILogger<IEmailCodeSend> logger)
        : JsonIntegrationEventHandler<EmailSendRecord>
    {
        private readonly IEmailSender _email = email ?? throw new ArgumentNullException(nameof(email));

        private readonly IEmailCodeSend _emailCodeSend =
            emailCodeSend ?? throw new ArgumentNullException(nameof(emailCodeSend));

        private readonly ILogger<IEmailCodeSend> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


        public override async Task Handler(EmailSendRecord notification)
        {
            await _emailCodeSend.SendEmailCodeAsync(notification.ToEmail, notification.Code.ToString());
            _logger.LogInformation("date:{Date},邮件发送{ToEmail}", DateTime.UtcNow, notification.ToEmail);
        }
    }

