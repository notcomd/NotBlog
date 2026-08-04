using Identity.Domain.Options;
using Identity.Infrastructure.Services;
using Notcomd.Token.JWT.Extensions;
using Identity.Domain.ICache;
using Identity.Infrastructure.Cache;

namespace Identity.Infrastructure;

public static class AddIdentityServer
{
    public static IServiceCollection AddIdentityService(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddJwtAuthentication(configuration);
        serviceCollection.AddScoped<IUserExternalLoginRepository, UserExternalLoginRepository>();
        serviceCollection.AddScoped<IOAuthService, OAuthService>();
        serviceCollection.AddScoped<IRoleGroupService, RoleGroupService>();
        //serviceCollection.AddScoped<IPermissionService, PermissionService>()
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<IUserRoleRepository, UserRoleRepository>();
        serviceCollection.AddScoped<IEmailCodeSend, EmailCodeSend>();
        serviceCollection.AddDistributedMemoryCache();
        serviceCollection.AddScoped<ISmsCodeSend, SmsCodeSend>();
        serviceCollection.AddScoped<UserService>();
        serviceCollection.AddScoped<IIdentityCacheService, IdentityCacheService>();



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