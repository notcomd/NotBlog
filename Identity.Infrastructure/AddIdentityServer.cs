using Identity.Domain.Options;
using Identity.Infrastructure.Services;

namespace Identity.Infrastructure;

public static class AddIdentityServer
{
    public static IServiceCollection AddIdentityService(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<IUserRoleRepository, UserRoleRepository>();
        serviceCollection.AddSingleton<IEmailCodeSend, EmailCodeSend>();
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddScoped<ISmsCodeSend, SmsCodeSend>();
        serviceCollection.AddScoped<UserService>();
        serviceCollection.AddJwtAuthentication(configuration);
        return serviceCollection;
    }

    public static IServiceCollection AddIdentityDbContext(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddDbContext<IdentityDbContext>(opt =>
        {
            var data = configuration.Get<DbContextOption>() ??
                       throw new ArgumentNullException("配置项为空", nameof(configuration));
            opt.UseNpgsql(data.DbContextConnect);
        });
        return serviceCollection;
    }
}