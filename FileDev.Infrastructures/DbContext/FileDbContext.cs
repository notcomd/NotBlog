using System.Reflection;

using DomainCommonst;

using FileDev.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Notcomd.DomainCommand;

using NotMediator;


namespace FileDev.Infrastructres.DbContext;


public class FileDbContext : BaseDbContext<FileDbContext>, IUnitOfWork
{
    private readonly INotMediator _notMediator;

    public FileDbContext(DbContextOptions<FileDbContext> options, INotMediator notMediator
        ) : base(options, notMediator)
    {

    }
    public DbSet<NotFile> Files { get; set; }
    public DbSet<FileGroup> FileGroup { get; set; }


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetEntryAssembly());
    }
}