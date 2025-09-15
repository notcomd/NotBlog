using System.Threading.Tasks;

using Identity.Infrastructure.EntityFramework;
using Identity.Web.API.Extensions;

using MimeKit.Cryptography;

namespace Identity.Web.API.Infrastructure;

public class UserDefullContextSeed : IDbSeeder<IdentityDbContext>
{

    private readonly IUserRoleRepository _userRoleRepository;

    public UserDefullContextSeed(IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
    }

    public async Task SeedAsync(IdentityDbContext context)
    {
        if (!context.Users.Any())
        {               
            await context.Users.AddAsync(await DefulUser());
            await context.SaveChangesAsync();
        }
    }

    public  async Task<User> DefulUser()
    {
        var role = await _userRoleRepository.FindByUserRoleAsync("Admin");
        if(role is null)
        {
            throw new InvalidOperationException("Admin role not found. Please seed roles first.");
        }
        var userObject= new User(role.Id,"Admin@notcomd.com","Admin@notcomd.com",DateTimeOffset.UtcNow);
        userObject.UserSafety.ChangeByUserStatus(EnumUserStatus.Normal);
        return userObject;
    }
}

