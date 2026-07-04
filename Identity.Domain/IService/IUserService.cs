namespace Identity.Domain.IService;

public interface IUserService
{
    /// <summary>
    /// 手机号登入验证
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <returns>成功返回 TokenResult（含 AccessToken + RefreshToken + 过期时间），失败返回 null</returns>
    Task<TokenResult?> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, string code);

    /// <summary>
    /// 邮箱登入验证
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <returns>成功返回 TokenResult（含 AccessToken + RefreshToken + 过期时间），失败返回 null</returns>
    Task<TokenResult?> LogInByCheckPasswordAsync([EmailAddress(ErrorMessage = "无效邮件地址")] string email, string password,
        string code);

    /// <summary>
    /// 用户注册（邮箱）
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="password">密码</param>
    /// <param name="code">验证码</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    Task<bool> RegisterByCreateUserAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email, string password,
        string code);

    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <param name="password">新密码</param>
    /// <param name="code">验证码</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    Task ChangeByPasswordAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email, string password, string code);

    /// <summary>
    /// 发送重置密码邮件
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>成功返回 true，失败返回 false</returns>
    Task SendResetPasswordEmailAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email);


    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱地址</param>
    /// <returns>用户实体</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns>用户列表</returns>
    Task<ICollection<User>> GetAllUsersAsync();
}