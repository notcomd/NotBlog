using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Dto.Request;

/// <summary>
/// 登录请求（邮箱 + 验证码或密码）。
/// 携带 <see cref="Password"/> 时走密码登入（开启二次验证需同时携带 <see cref="Code"/>）；
/// 仅携带 <see cref="Code"/> 时走邮箱验证码登入（未注册邮箱自动注册）。
/// </summary>
public record LoginRequest(
    [EmailAddress(ErrorMessage = "无效邮件地址")] string Email,
    string? Code,
    string? Password);