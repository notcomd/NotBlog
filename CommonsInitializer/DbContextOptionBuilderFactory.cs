using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CommonsInitializer;

public static class DbContextOptionBuilderFactory
{
    public static DbContextOptionsBuilder<TDbContext> Create<TDbContext>()
        where TDbContext : DbContext
    {
        var linkStr = Environment.GetEnvironmentVariable("DefaultConnection");
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(linkStr);
        return optionsBuilder;
    }


    public static DbContextOptionsBuilder<TDbContext> Create<TDbContext>(IConfiguration configuration)
        where TDbContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        return optionsBuilder;
    }
}