using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructures.EntityConfig;



public class MarkdownDbContext:DbContext
{
    
    public DbSet<MarkdownModel> MarkdownModels { get; set; }
    
    public MarkdownDbContext(DbContextOptions<MarkdownDbContext> options):base(options){}

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
    
}