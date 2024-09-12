using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        var data = configuration.GetSection(nameof(EmailAddress));
        
        serviceCollection.AddScoped<IEmail, Email>();
        serviceCollection.Configure<EmailAddress>(e =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                
            }else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                
            }else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                
            }
            //e.AddressHost=  
        });
        serviceCollection.AddOptions();
        return serviceCollection;
    }
    
    
}