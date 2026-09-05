namespace Identity.Domain.IRepository;

public interface IUserRepository : IRepository<User, IUnitOfWork>
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
    /// 获取全部用户（含 UserSafety / UserAccessFail 导航）
    /// </summary>
    Task<ICollection<User>> FindAllByUserAsync();

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
    /// 原子递增登录失败计数（S-13，ExecuteUpdate 并发安全）
    /// </summary>
    /// <param name="userGuid">用户ID</param>
    /// <returns>递增后的最新失败计数</returns>
    Task<int> IncrementAccessFaildCountAsync(Guid userGuid);

    /// <summary>
    /// 锁定用户登录失败记录（S-13，原子更新 LockOutEnd）
    /// </summary>
    Task LockUserAsync(Guid userGuid, DateTimeOffset lockOutEnd);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task UpdateByUserAsync(User user);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    Task DeleteByUserAsync(User user);

    /// <summary>
    /// 管理员分页查询用户（keyword 模糊匹配邮箱/用户名/手机号；返回安全投影，不含任何敏感字段）
    /// </summary>
    /// <param name="keyword">搜索关键字（可为空）</param>
    /// <param name="page">页码（从 1 开始）</param>
    /// <param name="pageSize">每页条数</param>
    /// <returns>分页用户列表</returns>
    Task<(ICollection<AdminUserBrief> Items, int Total)> GetPagedUsersAsync(string? keyword, int page, int pageSize);

    /// <summary>检查邮箱是否已被注册（管理员创建用户前的唯一性校验）</summary>
    Task<bool> ExistsByEmailAsync(string email);
}