
namespace Message.Domain.IRepository;

public interface IUserFollowRepository : IRepository<UserFollow, IUnitOfWork>
{
    Task<UserFollow?> GetAsync(Guid followerGuid, Guid followeeGuid);
    Task<bool> ExistsAsync(Guid followerGuid, Guid followeeGuid);

    /// <summary>我关注的人（分页）</summary>
    Task<IEnumerable<UserFollow>> GetFollowingAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>我的粉丝（分页）</summary>
    Task<IEnumerable<UserFollow>> GetFollowersAsync(Guid userGuid, int page = 1, int pageSize = 20);

    /// <summary>我关注的用户ID列表（Feed 聚合用）</summary>
    Task<IEnumerable<Guid>> GetFollowingIdsAsync(Guid userGuid);

    /// <summary>我的粉丝（关注者）用户ID列表</summary>
    Task<IEnumerable<Guid>> GetFollowerIdsAsync(Guid userGuid);

    Task<int> GetFollowingCountAsync(Guid userGuid);
    Task<int> GetFollowerCountAsync(Guid userGuid);

    Task<UserFollow> AddAsync(UserFollow follow);
    Task DeleteAsync(Guid followerGuid, Guid followeeGuid);
}
