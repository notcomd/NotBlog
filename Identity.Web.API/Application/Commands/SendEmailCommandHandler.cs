using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class SendEmailCommandHandler(IMailQueue mailQueue, ILogger<SendEmailCommandHandler> logger)
    :IRequestHandler<SendEmailCommand, bool>
{
    public Task<bool> Handler(SendEmailCommand request, CancellationToken cancellationToken)
    {

        logger.LogInformation("发送邮件请求，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);


        mailQueue.Enqueue(request.ToEmail, request.Subject, request.Body);
        logger.LogInformation("发送邮件已入队，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);
        return Task.FromResult(true);
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