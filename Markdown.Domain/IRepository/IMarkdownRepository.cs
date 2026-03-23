using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;

namespace Markdown.Domain.IRepository;

public interface IMarkdownRepository : IRepository<MarkDown>
{
    Task InsertMarkDownAsync(MarkDown markDown);

    Task<MarkDown?> FindMarkDownAsync(Guid markDownGuid);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(Guid userGuid);

    Task<MarkDown?> FindMarkDownAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(MarkDownAuth markDownAuth);

    Task<bool> UpdateMarkDownAsync(MarkDown markDown);

    Task<bool> DeleteMarkDownAsync(MarkDown markDown);
}