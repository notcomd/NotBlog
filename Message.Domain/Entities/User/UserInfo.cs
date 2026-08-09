namespace Message.Domain.Entities.User;

/// <summary>
/// 用户资料（聚合根，UserId 主键）。
/// <para>
/// 存储与账号/帖子正交的用户维数据：等级、硬币余额、背景封面（区别于头像）等，
/// 后续用户域拓展字段与方法在本聚合内演进。
/// </para>
/// </summary>
public class UserInfo : Entity<Guid>, IAggregateRoot
{
    private UserInfo()
    {
        UserId = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建用户资料（默认等级 1、硬币 0；同用户仅一条，主键唯一约束兜底）</summary>
    public static UserInfo Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        return new UserInfo
        {
            UserId = userId,
            Level = 1,
            Coins = 0
        };
    }

    /// <summary>对应用户 ID（Identity sub claim）</summary>
    public Guid UserId { get; init; }

    /// <summary>用户等级（默认 1，升级规则后续按活跃度/经验值接入）</summary>
    public int Level { get; private set; }

    /// <summary>硬币余额（投币/消费的账户口径，与推文 CoinCount 收款口径正交）</summary>
    public long Coins { get; private set; }

    /// <summary>背景封面 URL（区别于头像；FileDev 上传后存 URI，可空）</summary>
    public string? BackgroundCoverUrl { get; private set; }

    public DateTimeOffset CreateTime { get; init; }

    public DateTimeOffset UpdateTime { get; private set; }

    /// <summary>更新背景封面（空白视为清除）</summary>
    public void UpdateBackgroundCover(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && url.Length > 2048)
            throw new ArgumentException("背景封面URL不能超过2048个字符", nameof(url));

        BackgroundCoverUrl = string.IsNullOrWhiteSpace(url) ? null : url;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>增加硬币（数量必须大于 0）</summary>
    public void AddCoins(long amount)
    {
        if (amount <= 0)
            throw new ArgumentException("增加硬币数量必须大于0", nameof(amount));

        Coins += amount;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>扣除硬币（余额不足抛业务异常，调用方映射 400）</summary>
    public void ConsumeCoins(long amount)
    {
        if (amount <= 0)
            throw new ArgumentException("扣除硬币数量必须大于0", nameof(amount));
        if (Coins < amount)
            throw new InvalidOperationException("硬币余额不足");

        Coins -= amount;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>设置用户等级（不得小于 1；升级规则由后续业务驱动）</summary>
    public void SetLevel(int level)
    {
        if (level < 1)
            throw new ArgumentException("用户等级不能小于1", nameof(level));

        Level = level;
        UpdateTime = DateTimeOffset.UtcNow;
    }
}
