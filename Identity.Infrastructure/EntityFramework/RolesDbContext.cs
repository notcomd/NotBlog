using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.EntityFramework;

public class RolesDbContext : DbContext
{

    public RolesDbContext(DbContextOptions<RolesDbContext> options) : base(options)
    {

    }
    public DbSet<Roles> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Roles>().HasKey(en => en.RoleGuid);
    }
}