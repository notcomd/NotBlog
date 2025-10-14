


using DomainCommon;

using Markdown.Domain.Entities;
using Markdown.Domain.IRepository;
using Markdown.Infrastructures.DbContext;

using Microsoft.Extensions.Logging;

namespace Markdown.Infrastructres.Repository;

public class MarkReviewRepository : IMarkReviewRepository
{
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<MarkReviewRepository> _logger;
    private readonly MarkdownDbContext _markdownDbContext;

    public IUnitOfWork UnitOfWork => _markdownDbContext;

    public MarkReviewRepository(MarkdownDbContext dbContext, INotDateTime notDateTime, ILogger<MarkReviewRepository> logger)
    {
        _notDateTime = notDateTime;
        _logger = logger;
        _markdownDbContext = dbContext;
    }

    public Task<List<MarkReview>> GetMarkReviewsByMarkDownGroupIdAsync(Guid markDownGroupId)
    {
        throw new NotImplementedException();
    }

    public Task<List<MarkReview>> GetMarkReviewsByMarkDownUserIdAsync(Guid markDownUserId)
    {
        throw new NotImplementedException();
    }

    public Task<List<MarkReview>> GetMarkReviewsByMarkDownBlockIdAsync(Guid markDownBlockId)
    {
        throw new NotImplementedException();
    }


}