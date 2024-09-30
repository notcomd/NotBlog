using Identity.Domain.Entities;
using Identity.Domain.Option;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.EntityFramework;

public class UserDbContext : DbContext
{
    private readonly OptionsManager<DbContextOption> _optionsManager;

    public UserDbContext(DbContextOptions<UserDbContext> options, OptionsManager<DbContextOption> optionsManager) :
        base(options)
    {
        _optionsManager = optionsManager;
    }

    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>().HasKey(en => en.UserGuid);
        modelBuilder.Entity<User>().HasOne(en => en.UserAccessFail).WithOne(en => en.User)
            .HasForeignKey<UserAccessFail>(en => en.UserGuid);
    }
}