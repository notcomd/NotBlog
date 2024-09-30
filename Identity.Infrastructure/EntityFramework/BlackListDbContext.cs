using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.EntityFramework;

public class BlackListDbContext :DbContext
{
    public DbSet<BlackList> BlackLists { get; set; }

    private readonly IOptionsSnapshot<DbContextOptions> _optionsSnapshot;
    
    protected BlackListDbContext(){}
    public BlackListDbContext(DbContextOptions contextOptions):base(contextOptions){}

    public BlackListDbContext(DbContextOptions contextOptions,IOptionsSnapshot<DbContextOptions> optionsSnapshot) : base(contextOptions)
    {
        _optionsSnapshot = optionsSnapshot;
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(_optionsSnapshot.Value.ToString());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<BlackList>().HasKey(en=>en.BlackGuid);
        modelBuilder.Entity<BlackList>().HasOne(en => en.PhoneNumber).WithOne();
    }
}