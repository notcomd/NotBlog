namespace Message.Web.API.Dto.Response;
public class UserDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? NickName { get; init; }
    public string? Avatar { get; init; }
    public UserStatus Status { get; init; }
    public DateTime? LastOnlineTime { get; init; }
}

