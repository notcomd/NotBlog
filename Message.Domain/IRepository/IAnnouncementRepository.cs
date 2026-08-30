using Message.Domain.Entities.Announcement;

namespace Message.Domain.IRepository;

/// <summary>
/// 公报仓储接口（EF，MessageDbContext）
/// </summary>
public interface IAnnouncementRepository : IRepository<Announcement, IUnitOfWork>
{
    /// <summary>按 ID 取公报</summary>
    Task<Announcement?> GetByIdAsync(Guid announcementGuid);

    /// <summary>有效公报分页（未撤回，按创建时间倒序）</summary>
    Task<IEnumerable<Announcement>> GetActiveAsync(int page = 1, int pageSize = 20);

    /// <summary>有效公报总数</summary>
    Task<int> GetActiveCountAsync();

    /// <summary>全部公报分页（管理端，含已撤回，按创建时间倒序）</summary>
    Task<IEnumerable<Announcement>> GetPagedAsync(int page = 1, int pageSize = 20);

    /// <summary>全部公报总数</summary>
    Task<int> GetCountAsync();

    /// <summary>创建公报</summary>
    Task<Announcement> AddAsync(Announcement announcement);

    /// <summary>更新公报</summary>
    Task<Announcement> UpdateAsync(Announcement announcement);
}