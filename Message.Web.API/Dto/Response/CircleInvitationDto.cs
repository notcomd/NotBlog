namespace Message.Web.API.Dto.Response;

public class CircleInvitationDto
{
    public Guid InviteGuid { get; init; }
    public Guid CircleGuid { get; init; }
    public string CircleName { get; init; } = string.Empty;
    public Guid InviterGuid { get; init; }
    public Guid? InviteeGuid { get; init; }
    public string? Code { get; init; }
    public Guid? Token { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset ExpireTime { get; init; }
    public DateTimeOffset CreateTime { get; init; }
}

