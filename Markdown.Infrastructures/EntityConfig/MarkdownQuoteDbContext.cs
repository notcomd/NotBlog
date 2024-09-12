using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructures.EntityConfig;

public class MarkdownQuoteDbContext: DbContext
{
    public DbSet<MarkdownQuote> MarkdownQuoteModels { get; set; }

    public MarkdownQuoteDbContext(DbContextOptions<MarkdownQuoteDbContext> options) : base(options)
    {
        
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        base.OnModelCreating(modelBuilder);
        
    }
}