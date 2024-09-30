using Identity.Domain.IRepository;
using Identity.Infrastructure.EntityFramework;
using Identity.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notcomd.Token.JWT;

namespace Identity.Infrastructure;

public static class AddIdentityServer
{
    public static IServiceCollection AddIdentityService(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<IUserRoleRepository, UserRoleRepository>();
        serviceCollection.AddScoped<IEmailCodeSend, EmailCodeSend>();
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddScoped<ISmsCodeSend, SmsCodeSend>();
        serviceCollection.AddDbContext<UserRoleDbContext>();
        serviceCollection.AddDbContext<UserDbContext>();
        serviceCollection.AddJwtAuthentication(configuration);
        return serviceCollection;
    }
}