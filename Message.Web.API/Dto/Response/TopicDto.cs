namespace Message.Web.API.Dto.Response;

public class TopicDto
{
    public Guid TopicGuid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int PostCount { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

