using Identity.Domain.IRepository;
using Identity.Domain.Option;
using Identity.Domain.Server;
using Identity.Infrastructure.EntityFramework;
using Identity.Infrastructure.Repository;

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
        serviceCollection.AddScoped<UserRepositoryServer>();
        serviceCollection.AddEmailServer();
        serviceCollection.AddJwtAuthentication(configuration);
        return serviceCollection;
    }

    public static IServiceCollection AddIdentityDbContext(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddDbContext<UserDbContext>(opt =>
        {
            var data = configuration.Get<DbContextOption>() ??
                       throw new ArgumentNullException($"配置项为空", nameof(configuration));
            opt.UseNpgsql(data.DbContextConnect);
        });
        serviceCollection.AddDbContext<UserRoleDbContext>(opt =>
        {
            var data = configuration.Get<DbContextOption>() ??
                       throw new ArgumentNullException($"选项未配置", nameof(configuration));
            opt.UseNpgsql(data.DbContextConnect);
        });
        return serviceCollection;
    }
}