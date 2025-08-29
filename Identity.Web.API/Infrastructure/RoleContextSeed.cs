
using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.Extensions;

namespace Identity.Web.API.Infrastructure;

public class RoleContextSeed : IDbSeeder<IdentityDbContext>
{
    

    public async Task SeedAsync(IdentityDbContext context)
    {
        if(!context.Roles.Any())
        {
            context.Roles.AddRange(GetRolesType());
            _ =await context.SaveChangesAsync();
        }
        await context.SavaChangesAsync();
    }


    private static IEnumerable<Roles> GetRolesType()
    {

        yield return new Roles("Admin", "Administrator role", DateTimeOffset.UtcNow, EnRoleAuthority.Admin, EnRoleStatus.Normal);
        yield return new Roles("User", "Standard user role", DateTimeOffset.UtcNow, EnRoleAuthority.User, EnRoleStatus.Normal);
        yield return new Roles("Guest", "Guest user role", DateTimeOffset.UtcNow, EnRoleAuthority.Guest, EnRoleStatus.Normal);

    }
}
