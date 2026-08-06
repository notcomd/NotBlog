using Identity.Infrastructure.Idempotent;

namespace Identity.Web.API.Application.Commands;

public class SendEmailCommandHandler(IMailQueue mailQueue, ILogger<SendEmailCommandHandler> logger)
    :IRequestHandler<SendEmailCommand, bool>
{
    public Task<bool> Handler(SendEmailCommand request, CancellationToken cancellationToken)
    {
        // S-16：邮件正文不入日志（可能含验证码/敏感信息），仅记录收件人与主题
        logger.LogInformation("发送邮件请求，收件人：{ToEmail}，主题：{Subject}", request.ToEmail, request.Subject);

        // P6：邮件入后台队列发送（命令在事务内，不做 SMTP 外部 IO；
        // 入队不失败，顺带修复此前"改密成功但邮件发送失败导致整体 500"的问题）
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