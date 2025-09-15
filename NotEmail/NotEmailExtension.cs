using Microsoft.Extensions.DependencyInjection;

namespace EmailSendServer;

public static class NotEmailExtension
{
    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.AddOptions<EmailConfigurationOptions>();
        return serviceCollection;
    }

    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection,
        Action<EmailConfigurationOptions> emailConfiguration)
    {
        serviceCollection.Configure(emailConfiguration);
        serviceCollection.AddOptions<EmailConfigurationOptions>()
            .ValidateOnStart();
        // 正确获取 EmailOptions 配置节并绑定
        serviceCollection.AddScoped<IEmail, Email>();

        return serviceCollection;
    }
}