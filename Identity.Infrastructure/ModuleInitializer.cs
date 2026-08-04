using Commons.Core;
using Identity.Infrastructure.Idempotent;
using Identity.Infrastructure.Services;
using Identity.Domain.IService;
namespace Identity.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IUserRepository, UserRepository>();
        service.AddScoped<IUserRoleRepository, UserRoleRepository>();
        service.AddScoped<IRoleGroupRepository, RoleGroupRepository>();
        service.AddScoped<IPermissionRepository, PermissionRepository>();
        service.AddScoped<IEmailCodeSend, EmailCodeSend>();
        service.AddScoped<IRequestManagement, RequestManagement>();
        service.AddDistributedMemoryCache();
        if (!service.Any(s => s.ServiceType == typeof(IJwtTokenService)))
            service.AddScoped<IJwtTokenService, JwtTokenService>();
        //service.AddScoped<ISmsCodeSend, SmsCodeSend>();
        service.AddScoped<IUserService, UserService>();
        service.AddScoped<IUserRoleService, UserRoleService>();
        service.AddScoped<IRoleGroupService, RoleGroupService>();
        service.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        service.AddScoped<IOAuthService, OAuthService>();
        service.AddScoped<IPermissionChecker, PermissionChecker>();
    }
}