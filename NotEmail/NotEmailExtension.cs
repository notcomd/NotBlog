using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSendServer;

public static class NotEmailExtension
{
    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.AddOptions();
        return serviceCollection;
    }

    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var data = configuration.GetSection(nameof(EmailOptions));

        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.AddOptions();
        return serviceCollection;
    }
}