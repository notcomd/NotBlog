namespace Markdown.Web.API.Dto.Response;

/// <summary>
/// OldMarkDown 历史版本响应 DTO
/// </summary>
public class OldMarkDownResponse
{
    public Guid OldMarkDownGuid { get; set; }
    public Guid MarkDownGuid { get; set; }
    public Guid UserGuid { get; set; }
    public string Auth { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTimeOffset CreateAt { get; set; }
    public DateTimeOffset UpdateAt { get; set; }
}

/// <summary>
/// OldMarkDown 响应映射扩展
/// </summary>
public static class OldMarkDownMapper
{
    public static OldMarkDownResponse MapToOldMarkDownResponse(OldMarkDown old)
    {
        return new OldMarkDownResponse
        {
            OldMarkDownGuid = old.OldMarkDownGuid,
            MarkDownGuid = old.MarkDownGuid,
            UserGuid = old.UserGuid,
            Auth = old.AuthType.ToString(),
            Content = old.OldMarkDownContent,
            Hash = old.OldMarkDownHash,
            CreateAt = old.CreateAt,
            UpdateAt = old.UpdateAt
        };
    }
}
