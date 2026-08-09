namespace Message.Web.API.Dto.Request;

/// <summary>更新话题请求（R-12：创建者/管理员）。</summary>
public class UpdateTopicRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
