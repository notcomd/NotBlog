namespace Message.Web.API.Dto.Request;

/// <summary>更新个人签名请求（空白视为清除）。</summary>
public class UpdateUserBioRequest
{
    public string? Bio { get; init; }
}