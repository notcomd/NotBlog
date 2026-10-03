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

    /// <summary>创建用户资料（含邮箱、昵称、头像；默认等级 1、硬币 0）</summary>
    public static UserInfo Create(Guid userId, string email, string? nickName = null, Uri? avatarUrl = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("用户邮箱不能为空", nameof(email));

        return new UserInfo
        {
            UserId = userId,
            Email = email,
            NickName = nickName,
            AvatarUrl = avatarUrl,
            Level = 1,
            Coins = 0
        };
    }

    /// <summary>对应用户 ID（Identity sub claim）</summary>
    public Guid UserId { get; init; }

    /// <summary>用户邮箱</summary>
    public string Email { get; init;} = null!;

    /// <summary>用户昵称</summary>
    public string? NickName { get; private set; }

    /// <summary>用户等级（默认 1，升级规则后续按活跃度/经验值接入）</summary>
    public int Level { get; private set; }

    /// <summary>硬币余额（投币/消费的账户口径，与推文 CoinCount 收款口径正交）</summary>
    public long Coins { get; private set; }

    /// <summary>当前等级累计经验（升级清零，满级后继续累计展示用）</summary>
    public long Experience { get; private set; }

    /// <summary>背景封面 URL（区别于头像；FileDev 上传后存 URI，可空）</summary>
    public Uri? BackgroundCoverUrl { get; private set; }

    /// <summary>用户头像URL（可空）</summary>
    public Uri? AvatarUrl { get; private set; }

    /// <summary>个人签名（可空；空白视为未设置）</summary>
    public string? Bio { get; private set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreateTime { get; init; }

    /// <summary>更新时间</summary>
    public DateTimeOffset UpdateTime { get; private set; }

    /// <summary>更新个人签名（空白清除；不得超过 200 字符）</summary>
    public void UpdateBio(string? bio)
    {
        if (bio?.Length > 200)
            throw new ArgumentException("个人签名不能超过200个字符", nameof(bio));

        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>更新背景封面（空白视为清除）</summary>
    public void UpdateBackgroundCover(Uri? url)
    {
        if (url != null && url.ToString().Length > 2048)
            throw new ArgumentException("背景封面URL不能超过2048个字符", nameof(url));

        BackgroundCoverUrl = string.IsNullOrWhiteSpace(url?.ToString()) ? null : url;
        UpdateTime = DateTimeOffset.UtcNow;
    }


    /// <summary>更新头像（空白视为清除）</summary>
    public void UpdateAvatar(Uri? url)
    {
        if (url != null && url.ToString().Length > 2048)
            throw new ArgumentException("头像URL不能超过2048个字符", nameof(url));

        AvatarUrl = string.IsNullOrWhiteSpace(url?.ToString()) ? null : url;
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

    /// <summary>最高等级</summary>
    public const int MaxLevel = 9;

    /// <summary>从 level 级升到 level+1 级所需经验：500 × (5 × level) = 2500 × level（设计文档 3.1，口径 A）</summary>
    public static long LevelUpThreshold(int level) => 500L * 5 * level;

    /// <summary>
    /// 增加经验并自动升级（经验每级清零、剩余带入下一级）。
    /// </summary>
    /// <param name="amount">增加的经验值（必须大于 0）</param>
    /// <returns>本次升级的级数（0 表示未升级）</returns>
    public int AddExperience(long amount)
    {
        if (amount <= 0)
            throw new ArgumentException("增加经验必须大于0", nameof(amount));

        Experience += amount;
        var upgraded = 0;
        while (Level < MaxLevel && Experience >= LevelUpThreshold(Level))
        {
            Experience -= LevelUpThreshold(Level);
            Level++;
            upgraded++;
        }

        UpdateTime = DateTimeOffset.UtcNow;
        return upgraded;
    }
}
