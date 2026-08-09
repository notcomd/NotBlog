namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     MarkDown 实体 → 响应 DTO 映射
/// </summary>
public static class MarkdownResponseMapper
{
    public static MarkdownResponse MapToMarkdownResponse(MarkDown markdown) => new()
    {
        MarkDownGuid = markdown.MarkDownGuid,
        Name = markdown.MarkDownName,
        Content = markdown.MarkDownContent,
        Hash = markdown.MarkDownHash,
        Tags = [.. markdown.MarkDownTagboard],
        Auth = markdown.MarkDownAuth.ToString(),
        CreateAt = markdown.CreateAt,
        UpdateAt = markdown.UpdateAt
    };
}
