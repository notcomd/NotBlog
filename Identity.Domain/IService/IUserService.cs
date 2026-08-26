using Identity.Domain.Dto;

namespace Identity.Domain.IService;

public interface IUserService
{
    /// <summary>
    /// 邮箱验证码登录（统一登录/注册）。
    /// 校验邮箱验证码后：邮箱已存在则直接登录；不存在则自动创建账号并生成初始密码发送到该邮箱。
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="code">邮箱验证码</param>
    /// <returns>验证码错误或账号锁定等失败返回 null；否则返回 <see cref="EmailLoginResult"/>（含 Token 与是否新建账号）</returns>
    Task<EmailLoginResult?> LogInByEmailCodeAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")] string email, string code);

    /// <summary>
    /// 密码登入（邮箱 + 密码 + 二次验证）。
    /// 开启二次验证（<see cref="UserSafety.IsTwoFactorEnabled"/>）的用户必须携带邮箱验证码；
    /// 关闭二次验证的用户免验证码。邮箱不存在时返回 null（密码登入不做自动注册）。
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">邮箱验证码（仅开启二次验证的用户必填）</param>
    /// <returns>账号不存在、密码错误、验证码错误或账号锁定返回 null；否则返回 <see cref="EmailLoginResult"/>（IsNewUser 恒为 false）</returns>
    Task<EmailLoginResult?> LogInByPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")] string email, string password, string? code);


    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>用户实体</returns>
    Task<User?> GetUserInfoAsync(string email);

    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns>用户列表</returns>
    Task<ICollection<User>> FindUserByVagueAsync();

    /// <summary>
    /// 获取当前登录用户的二次验证开关状态。
    /// </summary>
    /// <param name="userGuid">用户ID（取自认证后的 NameIdentifier Claim）</param>
    /// <returns>用户不存在返回 null；否则返回 <see cref="UserSafety.IsTwoFactorEnabled"/></returns>
    Task<bool?> GetUserSafetyAsync(Guid userGuid);

    /// <summary>
    /// 更新当前登录用户的安全信息（如二次验证开关）。
    /// 关闭二次验证（置 false）为降级操作，必须携带 <see cref="UpdateUserSafetyRequest.Password"/> 或
    /// <see cref="UpdateUserSafetyRequest.Code"/> 之一进行二次确认。
    /// </summary>
    /// <param name="userGuid">用户ID（取自认证后的 NameIdentifier Claim）</param>
    /// <param name="isTwoFactorEnabled">是否开启二次验证；为 null 表示不更新该项</param>
    /// <param name="password">确认用密码（关闭二次验证时可作为二次确认，与 <paramref name="code"/> 二选一）</param>
    /// <param name="code">确认用邮箱验证码（关闭二次验证时可作为二次确认，与 <paramref name="password"/> 二选一）</param>
    /// <returns>用户不存在、无可更新内容或二次确认失败返回 false；成功返回 true</returns>
    Task<bool> UpdateUserSafetyAsync(Guid userGuid, bool? isTwoFactorEnabled, string? password, string? code);
}