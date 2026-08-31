namespace Message.Web.API.Dto.Response;

/// <summary>用户资料 DTO（等级/经验/硬币/背景封面/签到状态）。</summary>
public record UserInfoDto
{
    public Guid UserId { get; init; }

    public string? NickName { get; init; }

    public string Email { get; init; }= null!;

    /// <summary>用户等级</summary>
    public int Level { get; init; }

    /// <summary>硬币余额</summary>
    public long Coins { get; init; }

    /// <summary>当前等级累计经验（升级清零）</summary>
    public long Experience { get; init; }

    /// <summary>今日是否已签到</summary>
    public bool SignedInToday { get; init; }

    /// <summary>背景封面 URL（可空）</summary>
    public Uri? BackgroundCoverUrl { get; init; }

    public Uri? AvatarUrl { get; init; }

    /// <summary>个人签名（可空）</summary>
    public string? Bio { get; init; }
    
    public DateTimeOffset UpdateTime { get; init; }
}
