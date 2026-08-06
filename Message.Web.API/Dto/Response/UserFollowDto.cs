namespace Message.Web.API.Dto.Response;

public class UserFollowDto
{
    public Guid UserGuid { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

