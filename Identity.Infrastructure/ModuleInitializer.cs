using DomainCommons;
using DomainCommonst;

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
        service.AddScoped<UserRepositoryServer>();
    }
}