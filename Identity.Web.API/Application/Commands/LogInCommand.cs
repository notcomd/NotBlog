using System.ComponentModel.DataAnnotations;
using Notcomd.Token.JWT.Core;

namespace Identity.Web.API.Application.Commands;

/// <summary>
/// 统一登录命令（登录/注册二合一）：
/// 携带 <see cref="Password"/> 走密码登入（开启二次验证的用户需同时携带 <see cref="Code"/>）；
/// 仅携带 <see cref="Code"/> 走验证码登入——未注册邮箱自动注册并由注册命令生成初始密码邮件下发。
/// </summary>
public record LogInCommand(
    [EmailAddress(ErrorMessage = "无效邮件地址")] string Email,
    string? Password,
    string? Code) : IRequest<LogInCommandResult?>, ILoggableCommand
{
    public string IdProperty => nameof(Email);
    public string IdValue => Email;

    /// <summary>是否为密码登入。携带密码即视为密码登入；两通道同时携带时以密码登入为准，Code 作为二次验证码。</summary>
    public bool IsPasswordLogin => !string.IsNullOrWhiteSpace(Password);
}

/// <summary>
/// 统一登录命令结果。
/// </summary>
/// <param name="Token">登录成功签发的 Token；认证失败（验证码/密码错误、账号锁定、自动注册失败）时为 null</param>
/// <param name="IsNewUser">验证码登入并自动注册新账号时为 true</param>
/// <param name="UserId">本次登录用户 ID</param>
/// <param name="UserEmail">登录用户邮箱（供注册集成事件下发下游）</param>
/// <param name="UserName">登录用户昵称（供注册集成事件下发下游）</param>
/// <param name="AvatarUrl">登录用户头像（供注册集成事件下发下游）</param>
public record LogInCommandResult(TokenResult? Token, bool IsNewUser, Guid UserId,
    string? UserEmail = null, string? UserName = null, Uri? AvatarUrl = null,
    LoginFailureReason? FailureReason = null);

/// <summary>
/// 登录失败原因（成功时为 null）。
/// </summary>
public enum LoginFailureReason
{
    /// <summary>凭据错误：邮箱不存在、密码错误、账号锁定或验证码错误——对外统一文案，避免账号枚举。</summary>
    InvalidCredentials = 0,

    /// <summary>
    /// 需要邮箱验证码：密码校验已通过，但账号开启二次验证（<c>UserSafety.IsTwoFactorEnabled</c>，默认开启）
    /// 且未提供有效验证码。仅此原因对外可区分，供前端引导补填验证码。
    /// </summary>
    EmailCodeRequired = 1
}