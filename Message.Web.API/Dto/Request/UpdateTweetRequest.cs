namespace Message.Web.API.Dto.Request;
public class UpdateTweetRequest
{
    public string Content { get; init; } = string.Empty;
    public List<Guid>? FileIds { get; init; }
    public string? LinkUrl { get; init; }
    public string? Visibility { get; init; }
}

