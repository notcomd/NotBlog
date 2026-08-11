namespace Message.Domain.Entities.User;

/// <summary>
/// 用户签到记录（普通实体，非聚合根）。
/// <para>(UserId, SignInDate) 唯一约束防重复签到；签到发放固定经验由命令层负责。</para>
/// </summary>
public class UserSignIn : Entity<Guid>
{
    private UserSignIn()
    {
        Id = Guid.CreateVersion7();
        CreateTime = DateTimeOffset.UtcNow;
    }

    public UserSignIn(Guid userId, DateOnly signInDate) : this()
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));

        UserId = userId;
        SignInDate = signInDate;
    }

    public Guid UserId { get; init; }

    /// <summary>签到日期（UTC 自然日）</summary>
    public DateOnly SignInDate { get; init; }

    public DateTimeOffset CreateTime { get; init; }
}
