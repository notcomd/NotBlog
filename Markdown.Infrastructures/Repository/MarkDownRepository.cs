using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructures.EntityFramework;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructures.Repository;

public class MarkDownRepository : IMarkDownDbContextRepository<MarkDown>
{
    private readonly ILogger<IMarkdownRepository> _logger;
    private readonly MarkDownDbContext _markDownDbContext;

    public MarkDownRepository(MarkDownDbContext markDownDbContext, ILogger<IMarkdownRepository> logger)
    {
        _markDownDbContext = markDownDbContext;
        _logger = logger;
    }

    public async Task AddMarkDownAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<MarkDown> FindAsync(MarkDown entityDbcontext)
    {
        throw new NotImplementedException();
    }

    public async Task UpDataAsync(MarkDown entityDbcontext)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(MarkDown entityDbContext)
    {
        throw new NotImplementedException();
    }
}