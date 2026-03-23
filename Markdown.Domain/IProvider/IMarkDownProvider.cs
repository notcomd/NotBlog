using Markdown.Domain.Entities;

namespace Markdown.Domain.IProvider;

public interface IMarkDownProvider
{
    Task InsertMarkDownAsync(MarkDown markDown, CancellationToken cancellationToken);

    Task<MarkDown?> GetMarkDownByGuidAsync(Guid markDownGuid, CancellationToken cancellationToken);

    Task<MarkDown?> GetMarkDownByNameAsync(string markDownName, CancellationToken cancellationToken);

    Task<IEnumerable<MarkDown>> GetMarkDownsByUserGuidAsync(Guid userGuid, CancellationToken cancellationToken);
}