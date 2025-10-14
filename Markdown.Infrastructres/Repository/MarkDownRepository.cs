

using DomainCommon;

using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;

using Markdown.Infrastructures.DbContext;

using Microsoft.Extensions.Logging;
namespace Markdown.Infrastructres.Repository;


public class MarkDownRepository : IMarkdownRepository
{
    private readonly MarkdownDbContext _markDownDbContext;
    private readonly ILogger<IMarkdownRepository> _logger;

    private readonly INotDateTime _notDateTime;

    public MarkDownRepository(MarkdownDbContext markDownDbContext, ILogger<IMarkdownRepository> logger, INotDateTime notDateTime)
    {
        _markDownDbContext = markDownDbContext;
        _logger = logger;
        _notDateTime = notDateTime;
    }

    public Task<string> MarkDownUploadAsync(Stream fileStream, string markdownName)
    {
        throw new NotImplementedException();
    }

    public Task<MarkDown?> GetMarkDownAsync(Guid markDownGuid)
    {
        throw new NotImplementedException();
    }

    public Task<MarkDownGroup?> GetMarkDownGroupAsync(Guid markDownGroupGuid)
    {
        throw new NotImplementedException();
    }

    public IUnitOfWork UnitOfWork => throw new NotImplementedException();
}
