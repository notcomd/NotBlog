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
        // 注：仓储/服务等公共注册统一在 ModuleInitializer（AddNotBlogServices 自动扫描），
        // 此处仅注册本入口独有的服务，避免重复注册（此前 IUserRepository/IOAuthService/
        // AddDistributedMemoryCache 等重复注册且内存缓存覆盖 Redis）。
        serviceCollection.AddJwtAuthentication(configuration);
        serviceCollection.AddScoped<ISmsCodeSend, SmsCodeSend>();
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