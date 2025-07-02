using System.Reflection;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notcomd.DomainCommand;

namespace Identity.Infrastructure.EntityFramework;

public class UserDbContext : BaseDbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options, IMediator mediator) : base(options, mediator)
    {

    }

    public DbSet<User> Users { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<Roles> Roles { get; set; }
    public DbSet<NotClient> NotClients { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(en => en.UserGuid);
        modelBuilder.Entity<User>().HasOne(en => en.UserAccessFail)
            .WithOne(en => en.User)
            .HasForeignKey<UserAccessFail>(en => en.UserGuid);

        //UserAccessFail配置
        modelBuilder.Entity<UserAccessFail>().Property(en => en.LockOutEnd)
            .HasConversion(v => v.GetValueOrDefault()
                .ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        //Roles配置
        modelBuilder.Entity<Roles>().HasOne(en => en.User).WithOne()
            .HasForeignKey<Roles>(fr => fr.UserGuid);

        //UserRoles配置
        modelBuilder.Entity<UserRole>().HasKey(en => en.UserRoleGuid);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.Entity<UserRole>().HasOne(en => en.Roles);

        //NotClient配置
        modelBuilder.Entity<NotClient>().HasKey(en => en.NotClientId);

    }
}