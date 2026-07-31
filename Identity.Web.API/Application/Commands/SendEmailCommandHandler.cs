using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class SendEmailCommandHandler(IEmailCodeSend emailSender,ILogger<SendEmailCommandHandler> logger)
    :IRequestHandler<SendEmailCommand, bool>
{
    public async Task<bool> Handler(SendEmailCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"发送邮件请求，收件人：{request.ToEmail}，主题：{request.Subject}，内容：{request.Body}");

       // var emailMessage = new EmailMessage(request.ToEmail, request.Subject, request.Body, isHtml: false);

        var result = await emailSender.SendEmailCodeAsync(request.ToEmail, 
        request.Subject, request.Body);

        if (!result)
        {
            logger.LogError($"发送邮件失败，收件人：{request.ToEmail}，主题：{request.Subject}，内容：{request.Body}");
            throw new Exception("发送邮件失败");
        }
        logger.LogInformation($"发送邮件成功，收件人：{request.ToEmail}，主题：{request.Subject}，内容：{request.Body}");
        return result;
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