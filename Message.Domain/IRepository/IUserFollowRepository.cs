
namespace Message.Domain.IRepository;

/// <summary>
/// 用户关注仓储接口（UserFollow 聚合根）。
/// </summary>
public interface IUserFollowRepository : IRepository<UserFollow, IUnitOfWork>
{
    /// <summary>查询关注关系（不存在返回 null）</summary>
    Task<UserFollow?> GetAsync(Guid followerGuid, Guid followeeGuid);
    /// <summary>判断关注关系是否存在</summary>
    Task<bool> ExistsAsync(Guid followerGuid, Guid followeeGuid);

    /// <summary>我关注的人（分页）</summary>
    Task<IEnumerable<UserFollow>> GetFollowingAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>我的粉丝（分页）</summary>
    Task<IEnumerable<UserFollow>> GetFollowersAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>我关注的用户ID列表（Feed 聚合用）</summary>
    Task<IEnumerable<Guid>> GetFollowingIdsAsync(Guid userGuid);

    /// <summary>我的粉丝（关注者）用户ID列表</summary>
    Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userGuid);

    /// <summary>获取我关注的人数</summary>
    Task<int> GetFollowingCountAsync(Guid userGuid);
    /// <summary>获取我的粉丝数量</summary>
    Task<int> GetFollowerCountAsync(Guid userGuid);

    /// <summary>新增关注关系</summary>
    Task<UserFollow> AddAsync(UserFollow follow);
    /// <summary>删除关注关系</summary>
    Task DeleteAsync(Guid followerGuid, Guid followeeGuid);
}
