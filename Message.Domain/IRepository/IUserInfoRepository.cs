namespace Message.Domain.IRepository;

/// <summary>用户资料仓储（UserInfo 聚合根，UserId 主键）。</summary>
public interface IUserInfoRepository : IRepository<UserInfo, IUnitOfWork>
{
    /// <summary>按用户 ID 查询（不存在返回 null）</summary>
    Task<UserInfo?> GetByUserIdAsync(Guid userId);

    /// <summary>指定日期是否已签到（签到防重）</summary>
    Task<bool> IsSignedInAsync(Guid userId, DateOnly date);

    Task<UserInfo> AddAsync(UserInfo userInfo);

    Task<UserInfo> UpdateAsync(UserInfo userInfo);
}
