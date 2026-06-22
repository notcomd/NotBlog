namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// 根据用户ID获取用户
    /// </summary>
    /// <param name="guid">用户ID</param>
    /// <returns>用户</returns>
    ValueTask<User?> FindOneByUserAsync(Guid guid);

    /// <summary>
    /// 根据手机号获取用户
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>用户</returns>
    ValueTask<User?> FindOneByUserAsync(PhoneNumber phoneNumber);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <returns>用户</returns>
    ValueTask<User?> FindOneByUserAsync(string email);

    /// <summary>
    /// 添加用户
    /// </summary>
    /// <param name="user">用户</param>
    ValueTask AddOneByUserAsync(User user);

    /// <summary>
    /// 添加登录历史记录
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="message">登录消息</param>
    ValueTask AddByLoginHistoryAsync(PhoneNumber phoneNumber, string message);

    /// <summary>
    /// 保存手机号验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    ValueTask SaveByPhoneNumberAsync(PhoneNumber phoneNumber, string code);

    /// <summary>
    /// 从缓存中获取手机号验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>验证码</returns>
    ValueTask<string> RetirievePhoneCodeAsync(PhoneNumber phoneNumber);

    /// <summary>
    /// 从缓存中获取手机号
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>手机号</returns>
    ValueTask<string> FindPhoneNumberAsync(PhoneNumber phoneNumber);

    /// <summary>
    /// 保存邮箱验证码
    /// </summary>
    /// <param name="email">邮箱</param>
    /// <param name="code">验证码</param>
    ValueTask SaveByEmailNumberAsync(string email, string code);
}