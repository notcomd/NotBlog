using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Configuration;

public class UserRoleDbContextFactory : IDesignTimeDbContextFactory<UserRoleDbContext>
{
    public UserRoleDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserRoleDbContext>();
        options.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        return new UserRoleDbContext(options.Options);
    }
}