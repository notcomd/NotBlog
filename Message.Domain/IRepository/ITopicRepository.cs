
namespace Message.Domain.IRepository;

public interface ITopicRepository : IRepository<Topic, IUnitOfWork>
{
    Task<Topic?> GetByIdAsync(Guid topicGuid);
    Task<Topic?> GetByNameAsync(string name);

    /// <summary>有效话题列表（按帖子数降序，分页）</summary>
    Task<IEnumerable<Topic>> GetActiveAsync(int page = 1, int pageSize = 20);

    /// <summary>有效话题总数</summary>
    Task<int> GetActiveCountAsync();

    /// <summary>批量校验话题存在且有效（发帖时调用），返回存在的有效话题</summary>
    Task<IEnumerable<Topic>> GetExistingActiveAsync(IEnumerable<Guid> topicGuids);

    Task<Topic> AddAsync(Topic topic);
    Task<Topic> UpdateAsync(Topic topic);
    Task<bool> ExistsAsync(Guid topicGuid);
    Task<bool> NameExistsAsync(string name);
}
