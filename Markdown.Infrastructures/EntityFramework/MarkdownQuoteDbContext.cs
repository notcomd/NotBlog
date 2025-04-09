using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructures.EntityFramework;

public class MarkdownQuoteDbContext(DbContextOptions<MarkdownQuoteDbContext> options) : DbContext(options)
{
    public DbSet<MarkQuote> MarkQuote { get; set; }


    /// <summary>
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MarkQuote>().HasKey(en => en.MarkQuoteGuid);
        modelBuilder.Entity<MarkQuote>()
            .HasOne<MarkReview>(en => en.MarkReview).WithMany();
        modelBuilder.Entity<MarkQuote>()
            .HasOne<MarkDown>(en => en.Markdown).WithMany();
    }
}