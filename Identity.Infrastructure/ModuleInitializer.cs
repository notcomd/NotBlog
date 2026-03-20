using DomainCommons;
using DomainCommonst;
using Identity.Domain.IService;
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
        service.AddScoped<UserService>();
    }
}