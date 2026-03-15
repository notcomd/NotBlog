using System.Threading;
using System.Threading.Tasks;
using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using NotMediator;

namespace Markdown.Infrastructure.EntityFramework;

public class MarkDownDbContext(DbContextOptions<MarkDownDbContext> options,INotMediator notMediator) : DbContext(options), IUnitOfWork
{
    public DbSet<MarkDown> Markdowns { get; set; }

    
    
    
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }

    public Task<int> SavaChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<bool> SavaEntitiesAsync(CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}