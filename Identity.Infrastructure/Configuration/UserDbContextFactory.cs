using Identity.Infrastructure.EntityConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Configuration;

public class UserDbContextFactory: IDesignTimeDbContextFactory<UserDdContext>
{
    public UserDdContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UserDdContext>();
        options.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        return new UserDdContext(options.Options);
    }
}