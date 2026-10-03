
namespace Message.Domain.IRepository;

/// <summary>
/// 话题仓储接口（Topic 聚合根）。
/// </summary>
public interface ITopicRepository : IRepository<Topic, IUnitOfWork>
{
    /// <summary>按话题 ID 查询（不存在返回 null）</summary>
    Task<Topic?> GetByIdAsync(Guid topicGuid);
    /// <summary>按名称查询话题（不存在返回 null）</summary>
    Task<Topic?> GetByNameAsync(string name);

    /// <summary>有效话题列表（按帖子数降序，分页）</summary>
    Task<IEnumerable<Topic>> GetActiveAsync(int page = 1, int pageSize = 20);

    /// <summary>有效话题总数</summary>
    Task<int> GetActiveCountAsync();

    /// <summary>批量校验话题存在且有效（发帖时调用），返回存在的有效话题</summary>
    Task<IEnumerable<Topic>> GetExistingActiveAsync(IEnumerable<Guid> topicGuids);

    /// <summary>新增话题</summary>
    Task<Topic> AddAsync(Topic topic);
    /// <summary>更新话题</summary>
    Task<Topic> UpdateAsync(Topic topic);
    /// <summary>判断话题是否存在</summary>
    Task<bool> ExistsAsync(Guid topicGuid);
    /// <summary>判断话题名称是否已存在</summary>
    Task<bool> NameExistsAsync(string name);
}
