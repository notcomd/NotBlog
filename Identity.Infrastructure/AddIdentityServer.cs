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

        serviceCollection.AddEmailServer();
        serviceCollection.AddJwtAuthentication(configuration);
        return serviceCollection;
    }

    public static IServiceCollection AddIdentityDbContext(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        serviceCollection.AddDbContext<IdentityDbContext>(opt =>
            opt.UseNpgsql(configuration.Get<DbContextOption>()!.DbContextConnect ??
            throw new ArgumentNullException(nameof(configuration)),
                o => o.MigrationsAssembly("Identity.Infrastructure")));

        return serviceCollection;
    }
}