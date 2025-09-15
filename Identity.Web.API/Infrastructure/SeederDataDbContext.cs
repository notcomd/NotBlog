
using Identity.Infrastructure.EntityFramework;

namespace Identity.Web.API.Infrastructure;

public class SeederDataDbContext : IDbSeedDbContext<IdentityDbContext>
{

    private readonly IUserRoleRepository _userRoleRepository;
    private readonly INotDateTime _notDateOfTime;

    public SeederDataDbContext(IUserRoleRepository userRoleRepository, INotDateTime notDateOfTime)
    {
        _userRoleRepository = userRoleRepository;
        _notDateOfTime = notDateOfTime;
    }

    public async Task DbSeedAsync(IdentityDbContext context)
    {
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(GetRolesType());
            await context.SaveChangesAsync();
        }
        if (!context.Users.Any())
        {
            await context.Users.AddAsync(await GetUserAsync());
            await context.SaveChangesAsync();
        }


    }


    private static IEnumerable<Roles> GetRolesType()
    {
        yield return new Roles("Admin", "Administrator role", DateTimeOffset.UtcNow, EnRoleAuthority.Admin, EnRoleStatus.Normal);
        yield return new Roles("User", "Standard user role", DateTimeOffset.UtcNow, EnRoleAuthority.User, EnRoleStatus.Normal);
        yield return new Roles("Guest", "Guest user role", DateTimeOffset.UtcNow, EnRoleAuthority.Guest, EnRoleStatus.Normal);
    }

    public async Task<User> GetUserAsync()
    {
        var role = await _userRoleRepository.FindByUserRoleAsync("Admin");
        if (role is null)
        {
            throw new InvalidOperationException("Admin role not found. Please seed roles first.");
        }
        var userObject = new User(role.Id, "Admin@notcomd.com", "Admin@notcomd.com", _notDateOfTime.UtcNow);
        userObject.UserSafety.ChangeByUserStatus(EnUserStatus.Normal);
        return userObject;
    }

}
