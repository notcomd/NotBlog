namespace Markdown.Infrastructure.Provider;

public class MarkDownProvider : IMarkDownProvider
{
    public async Task InsertMarkDownAsync(MarkDown markDown, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<MarkDown?> GetMarkDownByGuidAsync(Guid markDownGuid, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<MarkDown?> GetMarkDownByNameAsync(string markDownName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<MarkDown>> GetMarkDownsByUserGuidAsync(Guid userGuid,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}