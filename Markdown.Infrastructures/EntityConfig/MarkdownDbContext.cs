using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructures.EntityConfig;

public class MarkdownDbContext : DbContext
{

    public MarkdownDbContext(DbContextOptions<MarkdownDbContext> options) : base(options) {}

    public DbSet<MarkDown> MarkdownModels { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MarkDown>().HasKey(en => en.MarkDownGuid);
    }
}