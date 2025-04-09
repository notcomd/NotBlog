using Microsoft.EntityFrameworkCore;
using Video.Domain.Entities;

namespace Video.Infrastructure.EntityFramework;

public class VideoDbContext : DbContext
{

    public VideoDbContext(DbContextOptions<VideoDbContext> options) : base(options)
    {

    }

    public DbSet<Videos> Videos { get; set; }

    public DbSet<VideoCollection> VideoCollections { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        // 实体映射
        //modelBuilder.SharedTypeEntity<VideoQuote>("VideoQuote");
        // modelBuilder.SharedTypeEntity<VideoControl>("VideoControl");
        // modelBuilder.SharedTypeEntity<VideoProtectedTime>("VideoProtectedTime");
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}