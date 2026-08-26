using Notcomd.Token.JWT.Core;

namespace Identity.Domain.Dto;

/// <summary>
/// 邮箱验证码登录（含自动注册）结果。
/// </summary>
/// <param name="Token">登录成功签发的 Token；验证码错误或账号锁定等失败场景为 null</param>
/// <param name="IsNewUser">该邮箱首次登录（本次自动创建了账号）时为 true；<see cref="UserId"/> 为新建用户 ID</param>
/// <param name="UserId">登录用户 ID</param>
public record EmailLoginResult(TokenResult? Token, bool IsNewUser, Guid UserId);