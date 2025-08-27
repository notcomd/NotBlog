
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

        yield return Roles.CreateByRoleAsync("Admin", "Administrator role", DateTimeOffset.UtcNow, EnumRoleAuthority.Admin, EnumRoleStatus.Normal);
        yield return Roles.CreateByRoleAsync("User", "Standard user role", DateTimeOffset.UtcNow, EnumRoleAuthority.User, EnumRoleStatus.Normal);
        yield return Roles.CreateByRoleAsync("Guest", "Guest user role", DateTimeOffset.UtcNow, EnumRoleAuthority.Guest, EnumRoleStatus.Normal);

    }
}
