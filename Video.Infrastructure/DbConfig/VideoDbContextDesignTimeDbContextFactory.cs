using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Video.Infrastructure.EntityFramework;

namespace Video.Infrastructure.DbConfig;

public class VideoDbContextDesignTimeDbContextFactory : IDesignTimeDbContextFactory<VideoDbContext>
{

    public VideoDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<VideoDbContext>();
        builder.UseNpgsql("Host=localhost;Database=video;Username=notcomd;Password=makefile");
        return new VideoDbContext(builder.Options);
    }
}