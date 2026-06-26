namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 创建用户命令
/// </summary>
public record RegisterByUserCommand(string PasswordHash, string Code, string? UserEmail, PhoneNumber? PhoneNumber)
    : IRequest<bool>;