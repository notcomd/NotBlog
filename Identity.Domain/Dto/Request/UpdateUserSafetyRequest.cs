namespace Identity.Domain.Dto.Request;

/// <summary>
/// 更新用户安全信息请求（登录后的已认证用户）。
/// <see cref="IsTwoFactorEnabled"/> 为 null 表示无需更新该项；传值则覆盖该项设置。
/// 关闭二次验证（置 false）为降级操作，必须携带 <see cref="Password"/> 或 <see cref="Code"/>
/// 其中的一种进行二次确认，防止账户被接管。
/// </summary>
public record UpdateUserSafetyRequest(bool? IsTwoFactorEnabled, string? Password, string? Code);