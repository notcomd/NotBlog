namespace Markdown.Web.API.Dto.Response;

/// <summary>
///     收藏响应（含文章名称，便于收藏列表直接展示）
/// </summary>
public class MarkFavoriteResponse
{
    public Guid MarkFavoriteGuid { get; set; }

    public Guid MarkDownGuid { get; set; }

    public string MarkDownName { get; set; } = null!;

    public List<string> Tags { get; set; } = [];

    public DateTimeOffset CreateAt { get; set; }
}
