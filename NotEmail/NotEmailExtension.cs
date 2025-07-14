using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSendServer;

public static class NotEmailExtension
{
    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.AddOptions<EmailOptions>();
        return serviceCollection;
    }

    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        // 正确获取 EmailOptions 配置节并绑定
        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.Configure<EmailOptions>(configuration.GetSection(nameof(EmailOptions)));
        return serviceCollection;
    }
}