using System.Reflection;

using DomainCommonst;

using FileDev.Domain.DomainEntities;
using FileDev.Infrastructres.EntityConfigurtion;

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
    
    public DbSet<NotFileRepository> NotFileRepository { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new FileGroupEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NotFileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new NotFileRepositoryEntityConfiguration());
    }



}