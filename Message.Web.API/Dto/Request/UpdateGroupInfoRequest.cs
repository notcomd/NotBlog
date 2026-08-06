namespace Message.Web.API.Dto.Request;
public class UpdateGroupInfoRequest
{
    public string GroupName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

