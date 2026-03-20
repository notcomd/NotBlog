namespace Identity.Domain.IService;

public interface IUserService
{
    /// <summary>
    ///  登入验证
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public Task<string> LogInByCheckPasswordAsync(PhoneNumber phoneNumber, string password, long code);

    /// <summary>
    ///  登入验证
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public Task<string> LogInByCheckPasswordAsync(
        [EmailAddress(ErrorMessage = "无效邮件地址")]
        string email,
        string password,
        string code);

    /// <summary>
    ///  创建用户
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public Task<bool> SignInByCreateUserAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email, string password,
        string code);

    /// <summary>
    ///  重置密码
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public Task ResetPasswordAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email, string password, string code);

    /// <summary>
    ///  发送重置密码邮件
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task SendResetPasswordEmailAsync([EmailAddress(ErrorMessage = "无效的邮件地址")] string email);

    /// <summary>
    ///  获取授权链接
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="redirectUri"></param>
    /// <returns></returns>
    public Task<string> GenerateAuthorizationUrlAsync(string provider, string redirectUri);
}