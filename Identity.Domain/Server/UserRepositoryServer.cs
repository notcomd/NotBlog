using Identity.Domain.IRepository;

namespace Identity.Domain.Server;

public class UserRepositoryServer
{

    private readonly IUserRepository _userRepository ;

    private readonly IUserRoleRepository _userRoleRepository;

    public UserRepositoryServer(IUserRepository userRepository, IUserRoleRepository userRoleRepository)
    {
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
    }

    public ValueTask<string> LogInByCheckPasswordAsync()
    {

        return new ValueTask<string>("sdf");
    } 
    
    
    
    
}