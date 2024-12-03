using Identity.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Identity.Infrastructure.Configuration;

public class RolesDbContextFactory : IDesignTimeDbContextFactory<RolesDbContext>
{
    public RolesDbContext CreateDbContext(string[] args)
    {
        var build = new DbContextOptionsBuilder<RolesDbContext>();
        build.UseNpgsql("Host=localhost;Database=identityuser;Username=notcomd;Password=makefile");
        return new RolesDbContext(build.Options);
    }
}