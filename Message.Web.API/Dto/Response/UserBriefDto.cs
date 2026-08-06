namespace Message.Web.API.Dto.Response;
public class UserBriefDto
{
    public Guid UserGuid { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? Avatar { get; init; }
}

