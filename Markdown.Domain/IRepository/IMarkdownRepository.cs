using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;

namespace Markdown.Domain.IRepository;

public interface IMarkdownRepository : IRepository<MarkDown>
{
    /// <summary>
    /// 获取追踪状态的 MarkDown 实体（用于更新操作）
    /// </summary>
    Task<MarkDown?> GetMarkDownTrackedAsync(Guid markDownGuid);

    Task<MarkDown?> FindMarkDownAsync(Guid markDownGuid);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(Guid userGuid);

    Task<MarkDown?> FindMarkDownAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(MarkDownAuth markDownAuth);
}