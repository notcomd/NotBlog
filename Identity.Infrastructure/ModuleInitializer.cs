using DomainCommonst;
using Identity.Domain.IRepository;

using Identity.Infrastructure.Repository;
using Identity.Infrastructure.RequestManager;

using Microsoft.Extensions.DependencyInjection;

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
        service.AddScoped<IRequestManager, RequestManager.RequestManager>();
        service.AddScoped<INotDateTime, NotDateTime.NotDateTime>();
        //service.AddScoped<UserRepositoryServer>();
    }
}