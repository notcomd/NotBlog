namespace Message.Web.API.Dto.Request;

public class CreateCircleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
    /// <summary>频道封面（图片/动图/视频 URL）</summary>
    public string? CoverUrl { get; init; }
    /// <summary>成员上限（默认 500）</summary>
    public int? MaxMembers { get; init; }
}

