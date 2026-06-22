using DomainCommons;
using Identity.Infrastructure.Services;

namespace Identity.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IUserRepository, UserRepository>();
        service.AddScoped<IUserRoleRepository, UserRoleRepository>();
        service.AddScoped<IEmailCodeSend, EmailCodeSend>();
        service.AddDistributedMemoryCache();
        service.AddScoped<ISmsCodeSend, SmsCodeSend>();
        service.AddScoped<IUserService, UserService>();
        service.AddScoped<IUserRoleService, UserRoleService>();
        service.AddScoped<IRoleGroupService, RoleGroupService>();
        service.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        service.AddScoped<IOAuthService, OAuthService>();
        service.AddScoped<IGitHubAuthService, GithubAuthService>();
    }
}