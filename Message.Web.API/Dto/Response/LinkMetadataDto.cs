namespace Message.Web.API.Dto.Response;
public class LinkMetadataDto
{
    public string Url { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Image { get; init; }
}

