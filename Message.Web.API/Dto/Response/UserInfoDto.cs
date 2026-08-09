namespace Message.Web.API.Dto.Response;

/// <summary>用户资料 DTO（等级/硬币/背景封面）。</summary>
public class UserInfoDto
{
    public Guid UserId { get; init; }
    /// <summary>用户等级</summary>
    public int Level { get; init; }
    /// <summary>硬币余额</summary>
    public long Coins { get; init; }
    /// <summary>背景封面 URL（可空）</summary>
    public string? BackgroundCoverUrl { get; init; }
    public DateTimeOffset UpdateTime { get; init; }
}
