using System.Reflection;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.EntityFramework;

public class UserRoleDbContext : DbContext
{
    public UserRoleDbContext(DbContextOptions<UserRoleDbContext> options) : base(options)
    {
    }

    public DbSet<UserRole> UserRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UserRole>().HasKey(en => en.UserRoleGuid);
        modelBuilder.Entity<UserRole>(
            en => en.Property(ens => ens.RoleEndTime)
                .HasColumnType("timestamp with time zone"));
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}