using Identity.Domain.IRepository;
using Identity.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notcomd.Token.JWT;

namespace Identity.Infrastructure;

public static class AddIdentityServer
{
    public static IServiceCollection AddIdentity(this IServiceCollection serviceCollection,IConfiguration configuration)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<IUserRoleRepository, UserRoleRepository>();
        serviceCollection.AddScoped<IEmailCodeSend, EmailCodeSend>();
        serviceCollection.AddScoped<ISmsCodeSend, SmsCodeSend>();
        serviceCollection.AddJwtAuthentication(configuration.GetSection(nameof(Notcomd_JwtOptions)));
        return serviceCollection;
    }
}