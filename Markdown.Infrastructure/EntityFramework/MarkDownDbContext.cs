using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NotMediator;

namespace Markdown.Infrastructure.EntityFramework;

public class MarkDownDbContext(DbContextOptions<MarkDownDbContext> options,INotMediator notMediator) : DbContext(options)
{
    public DbSet<MarkDown> Markdowns { get; set; }

    
    
    
    
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
    }
}