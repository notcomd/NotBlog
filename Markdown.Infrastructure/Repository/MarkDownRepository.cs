using System;
using System.Threading.Tasks;
using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructure.EntityFramework;
using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructures.Repository;

public class MarkDownRepository(MarkDownDbContext markDownDbContext, ILogger<IMarkdownRepository> logger)
    : IMarkDownDbContextRepository<MarkDown>
{
    private readonly ILogger<IMarkdownRepository> _logger = logger;
    private readonly MarkDownDbContext _markDownDbContext = markDownDbContext;

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