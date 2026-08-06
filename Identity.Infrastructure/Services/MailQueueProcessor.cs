namespace Identity.Infrastructure.Services;

/// <summary>
/// 邮件队列后台消费者：逐条读取 MailQueue 并通过 IEmailCodeSend 发送。
/// 发送失败仅记日志（下次同类邮件由业务重新触发），不重试堆积。
/// </summary>
public class MailQueueProcessor(
    MailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<MailQueueProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[MailQueue] 邮件后台发送服务已启动");

        await foreach (var task in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailCodeSend>();
                var success = await emailSender.SendEmailCodeAsync(task.ToEmail, task.Subject, task.Body);

                if (!success)
                {
                    logger.LogWarning("[MailQueue] 邮件发送失败: To={ToEmail}, Subject={Subject}",
                        task.ToEmail, task.Subject);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[MailQueue] 邮件发送异常: To={ToEmail}, Subject={Subject}",
                    task.ToEmail, task.Subject);
            }
        }
    }
}
