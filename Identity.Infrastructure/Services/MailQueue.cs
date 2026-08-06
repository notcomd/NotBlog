using System.Threading.Channels;

namespace Identity.Infrastructure.Services;

/// <summary>
/// 基于 Channel 的进程内邮件队列（有界，满则丢弃——邮件失败可重发，不影响业务事务）
/// </summary>
public class MailQueue : IMailQueue
{
    private readonly ILogger<MailQueue> _logger;
    private readonly Channel<MailTask> _channel = Channel.CreateBounded<MailTask>(
        new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true
        });

    public MailQueue(ILogger<MailQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>后台处理器读取通道（仅 MailQueueProcessor 使用）</summary>
    internal ChannelReader<MailTask> Reader => _channel.Reader;

    public void Enqueue(string toEmail, string subject, string body)
    {
        if (!_channel.Writer.TryWrite(new MailTask(toEmail, subject, body)))
        {
            _logger.LogWarning("[MailQueue] 队列已满，邮件已丢弃: To={ToEmail}, Subject={Subject}",
                toEmail, subject);
        }
    }

    /// <summary>待发送邮件任务</summary>
    public sealed record MailTask(string ToEmail, string Subject, string Body);
}
