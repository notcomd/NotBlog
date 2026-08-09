namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     OldMarkDown 历史版本实体 → 响应 DTO 映射
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
