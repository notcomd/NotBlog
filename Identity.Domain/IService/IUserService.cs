namespace Identity.Domain.IService;

public interface IUserService
{
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