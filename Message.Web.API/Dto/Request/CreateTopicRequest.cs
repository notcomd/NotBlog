namespace Message.Web.API.Dto.Request;

/// <summary>创建话题</summary>
public class CreateTopicRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

