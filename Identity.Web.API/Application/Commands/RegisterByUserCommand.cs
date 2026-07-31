namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 创建用户命令
/// </summary>
public record RegisterByUserCommand(string PasswordHash, string Code, string? UserEmail)
    : IRequest<bool>, ILoggableCommand
{
    public string IdProperty => nameof(UserEmail);
    public string IdValue => UserEmail ?? string.Empty;
}