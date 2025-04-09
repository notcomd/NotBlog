using Microsoft.EntityFrameworkCore;
using File = FileDev.Domain.Entities.File;

namespace ConsoleApp1.EntityConfig;

public class FileDbContext : DbContext
{

    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options)
    {

    }
    public DbSet<File> Files { get; set; }

    /// <summary>
    ///     配置dbcontext 内容
    /// </summary>
    /// <param name="configurationBuilder"></param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }
}