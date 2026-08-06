namespace Message.Web.API.Dto.Request;

/// <summary>加入圈子：code（邀请码）与 token（链接）二选一</summary>
public class JoinCircleRequest
{
    public string? Code { get; init; }
    public Guid? Token { get; init; }
}

