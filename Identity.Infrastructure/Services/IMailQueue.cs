namespace Identity.Infrastructure.Services;

/// <summary>
/// 邮件发送后台队列（P6：领域事件 handler / 命令在事务内不再直接发 SMTP，
/// 改为 O(1) 入队，由 MailQueueProcessor 后台消费发送，避免外部 IO 占用数据库事务）
/// </summary>
public interface IMailQueue
{
    /// <summary>
    /// 投递邮件到后台发送队列。队列满时丢弃并记警告（邮件非关键路径，业务不受影响）。
    /// </summary>
    void Enqueue(string toEmail, string subject, string body);
}
