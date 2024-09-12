using System.Reflection;
using Identity.Domain.Entities;
using Identity.Domain.Option;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.EntityConfig;

public class UserDdContext: DbContext
{
    public DbSet<User> Users { get; set; }

    private readonly OptionsManager<DbContextOption> _optionsManager;
    public UserDdContext(DbContextOptions<UserDdContext> options, OptionsManager<DbContextOption> optionsManager) :
        base(options)
    {
        _optionsManager = optionsManager;
    }
     
    public UserDdContext(DbContextOptions<UserDdContext> options):base(options){}
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasKey(en => en.UserGuid);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}