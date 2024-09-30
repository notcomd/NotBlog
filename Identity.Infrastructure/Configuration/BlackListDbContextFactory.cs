using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Configuration;

public class BlackListDbContextFactory:IDesignTimeDbContextFactory<BlackListDbContext>
{
    public BlackListDbContext CreateDbContext(string[] args)
    {
        var build = new DbContextOptionsBuilder();
        build.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        return new BlackListDbContext(build.Options);
    }
}