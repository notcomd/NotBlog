using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailSendServer;

public static class AddScopedOption
{
    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection)
    {

        serviceCollection.AddScoped<IEmail, Email>();
        // serviceCollection.AddScoped<MailPush>();
        return serviceCollection;
    }

    public static IServiceCollection AddEmailServer(this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        var data = configuration.GetSection(nameof(EmailSetting));

        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.AddOptions();
        return serviceCollection;
    }
}