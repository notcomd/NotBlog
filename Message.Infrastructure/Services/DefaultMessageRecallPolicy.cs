using Message.Domain.IServices;

namespace Message.Infrastructure.Services;

/// <summary>
/// 默认消息撤回策略：固定撤回时限 2 分钟。
/// </summary>
public class DefaultMessageRecallPolicy : IMessageRecallPolicy
{
    /// <summary>默认撤回时限：2 分钟</summary>
    public static readonly TimeSpan DefaultTimeLimit = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public bool CanRecall(DateTime sentTime)
    {
        var timeDiff = DateTime.UtcNow - sentTime;
        return timeDiff <= DefaultTimeLimit;
    }
}
