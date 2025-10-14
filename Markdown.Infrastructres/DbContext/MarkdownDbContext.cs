using System.Reflection;

using CommonsInitializer;

using DomainCommon;

using Markdown.Domain.Entities;

using Microsoft.EntityFrameworkCore;

using Notcomd.DomainCommand;

using NotMediator;



namespace Markdown.Infrastructures.DbContext;

public class MarkdownDbContext : BaseDbContext<MarkdownDbContext>, IUnitOfWork
{

    public MarkdownDbContext(DbContextOptions<MarkdownDbContext> options, INotMediator mediator) :
        base(options, mediator)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));

        }
        PintIcon.PrintPng();
    }

    public DbSet<MarkReview> MarkReview { get; set; }
    public DbSet<MarkDown> MarkDown { get; set; }
    public DbSet<MarkDownGroup> MarkDownGroup { get; set; }
    public DbSet<BlockVersion> BlockVersion { get; set; }

    /// <summary>
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}