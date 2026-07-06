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
        string? code);


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
}