namespace Message.Web.API.Dto.Response;

public class CircleMemberDto
{
    public Guid UserGuid { get; init; }
    public string Role { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public DateTimeOffset JoinTime { get; init; }
}

