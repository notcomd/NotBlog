using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class SendEmailCommandHandler(IEmailSender emailSender)
    : NotMediator.IRequestHandler<SendEmailCommand, bool>
{
    public async Task<bool> Handler(SendEmailCommand request, CancellationToken cancellationToken)
    {
        var emailMessage = new EmailMessage(request.ToEmail, request.Subject, request.Body);

        var result = await emailSender.SendAsync(emailMessage, cancellationToken);

        return result.Success;
    }
}

public class SendEmailIdentifiedCommandHandler(
    ILogger<IdentifiedCommandHandler<SendEmailCommand, bool>> logger,
    INotMediator mediator,
    IRequestManagement requestManagement)
    : IdentifiedCommandHandler<SendEmailCommand, bool>(logger, mediator, requestManagement)
{
    protected override bool CreateResultForDuplicateRequest()
    {
        return true;
    }
}