using Markdown.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Markdown.Infrastructures.EntityFramework;

public class MarkDownDbContext(DbContextOptions<MarkDownDbContext> options) : DbContext(options)
{

    public DbSet<MarkDown> Markdowns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarkDown>().HasKey(x => x.MarkDownGuid);
        modelBuilder.Entity<MarkDown>().Property(x => x.MarkDownGuid).HasColumnName("MarkDownGuid");
        modelBuilder.Entity<MarkDown>().Property(x => x.MarkDownName).HasColumnName("MarkDownName");
        modelBuilder.Entity<MarkDown>().Property(x => x.MarkDownTagboard).HasColumnName("MarkDownTagboard");

        modelBuilder.Entity<MarkDown>().HasMany(en => en.MarkReview)
            .WithOne(be => be.MarkDown)
            .HasForeignKey(fr => fr.MarkDownGuid);
    }
}