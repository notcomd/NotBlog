namespace Message.Web.API.Dto.Response;

/// <summary>他人/本人用户公开信息 DTO（个人主页聚合：资料 + 关注统计 + 作品统计）。</summary>
public class UserProfileDto
{
    public Guid UserGuid { get; init; }

    public string? NickName { get; init; }

    /// <summary>个人签名（可空）</summary>
    public string? Bio { get; init; }

    public Uri? AvatarUrl { get; init; }

    /// <summary>关注数</summary>
    public int FollowingCount { get; init; }

    /// <summary>粉丝数</summary>
    public int FollowerCount { get; init; }

    /// <summary>已发布作品数（对查看者可见口径）</summary>
    public int PostCount { get; init; }

    /// <summary>作品获赞总数（已发布作品 LikeCount 之和）</summary>
    public long LikeTotal { get; init; }

    /// <summary>当前查看者是否已关注该用户（未认证恒为 false）</summary>
    public bool IsFollowing { get; init; }
}