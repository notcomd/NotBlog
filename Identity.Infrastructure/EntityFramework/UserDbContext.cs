using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.EntityFramework;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasKey(en => en.UserGuid);
        modelBuilder.Entity<User>().HasOne(en => en.UserAccessFail).WithOne(en => en.User)
            .HasForeignKey<UserAccessFail>(en => en.UserGuid);
        // modelBuilder.Entity<User>(en =>
        //{
        // en.Property(ens => ens.CreateDatetime).HasConversion(v=>v.ToUniversalTime(),v=>DateTime.SpecifyKind(v,DateTimeKind.Utc));
        // en.Property(ens => ens.UserAccessFail.LockOutEnd).HasColumnType("timestamp with time zone")
        //});
        //modelBuilder.Entity<User>().HasOne(en=>en.Roles).WithOne(en=>en.);
        modelBuilder.Entity<UserAccessFail>().Property(en => en.LockOutEnd)
            .HasConversion(v => v.GetValueOrDefault().ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    }
}