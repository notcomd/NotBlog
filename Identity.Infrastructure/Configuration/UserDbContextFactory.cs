using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Configuration;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserDbContext>();
        options.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        return new UserDbContext(options.Options);
    }
}