namespace Identity.Web.API.Application.Commands;

/// <param name="Code">邮箱验证码（hasCode 路径必填，将在 Handler 中校验）</param>
public record ChangeByPasswordCommand(Guid UserId, string NewPassword, string? Code) : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(UserId);
    public string IdValue => UserId.ToString();
}