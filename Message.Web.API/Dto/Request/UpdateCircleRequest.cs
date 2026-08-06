namespace Message.Web.API.Dto.Request;

public class UpdateCircleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? AvatarUrl { get; init; }
}

