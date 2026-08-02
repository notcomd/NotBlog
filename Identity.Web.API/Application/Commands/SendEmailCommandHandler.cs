using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class SendEmailCommandHandler(IEmailCodeSend emailSender,ILogger<SendEmailCommandHandler> logger)
    :IRequestHandler<SendEmailCommand, bool>
{
    public async Task<bool> Handler(SendEmailCommand request, CancellationToken cancellationToken)
    {
        // S-16：邮件正文不入日志（可能含验证码/敏感信息），仅记录收件人与主题
        logger.LogInformation("发送邮件请求，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);

        var result = await emailSender.SendEmailCodeAsync(request.ToEmail,
        request.Subject, request.Body);

        if (!result)
        {
            logger.LogError("发送邮件失败，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);
            throw new Exception("发送邮件失败");
        }
        logger.LogInformation("发送邮件成功，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);
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