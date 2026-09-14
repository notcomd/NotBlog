namespace Message.Domain.IRepository;

/// <summary>用户资料仓储（UserInfo 聚合根，UserId 主键）。</summary>
public interface IUserInfoRepository : IRepository<UserInfo, IUnitOfWork>
{
    /// <summary>按用户 ID 查询（不存在返回 null）</summary>
    Task<UserInfo?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// 按邮箱精确查询（忽略大小写；不存在返回 null）。
    /// <para>供「按邮箱查找用户」使用：仅精确匹配，不做模糊查询，避免批量枚举账号。</para>
    /// </summary>
    Task<UserInfo?> GetByEmailAsync(string email);

    /// <summary>
    /// 按昵称精确查询（忽略大小写，最多返回 limit 条）。
    /// <para>昵称允许重名，故返回集合；调用方负责限制条数上限。</para>
    /// </summary>
    Task<IEnumerable<UserInfo>> GetByNickNameAsync(string nickName, int limit);

    /// <summary>指定日期是否已签到（签到防重）</summary>
    Task<bool> IsSignedInAsync(Guid userId, DateOnly date);

    Task<UserInfo> AddAsync(UserInfo userInfo);

    Task<UserInfo> UpdateAsync(UserInfo userInfo);
}
