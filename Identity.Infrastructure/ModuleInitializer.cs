using DomainCommonst;

using Identity.Domain.Server;
using Identity.Infrastructure.NotMemoryCache;
using Identity.Infrastructure.RequestManager;

namespace Identity.Infrastructure;

public class ModuleInitializer : IModuleInitializer
{

    public void Initialize(IServiceCollection service)
    {
        service.AddScoped<IUserRepository, UserRepository>();
        service.AddScoped<IUserRoleRepository, UserRoleRepository>();
        service.AddScoped<IEmailCodeSend, EmailCodeSend>();
        service.AddDistributedMemoryCache();    
        service.AddScoped<INotMemoryCache, NotMemoryCache.NotMemoryCache>();
        service.AddScoped<ISmsCodeSend, SmsCodeSend>();
        service.AddScoped<IRequestManager, RequestManager.RequestManager>();
        service.AddScoped<INotDateTime, NotDateTime.NotDateTime>();
        service.AddScoped<IdentityDomainCheckLogInServer>();
        service.AddScoped<IdentityDomainRegisterServer>();
        service.AddScoped<IdentityDomainUserManagerServer>();
        service.AddScoped<IdentityDomainToolServer>();
        service.AddScoped<IdentityDomainRoleManagerServer>();
    }
}