using System.Reflection;
using Identity.Domain.Entities;
using Identity.Domain.Option;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.EntityConfig;

public class UserRoleDbContext: DbContext
{
    public DbSet<UserRole> UserRoles { get; set; }

    private readonly OptionsManager<DbContextOption> _optionsManager;

    public UserRoleDbContext(DbContextOptions<UserRoleDbContext> options,
        OptionsManager<DbContextOption> optionsManager) : base(options)
    {
        _optionsManager = optionsManager;
    }
    public UserRoleDbContext(DbContextOptions<UserRoleDbContext> options):base(options){}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserRole>().HasKey(en => en.UserRoleGuid);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}