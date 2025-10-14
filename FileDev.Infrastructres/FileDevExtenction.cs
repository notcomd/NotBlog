using FileDev.Domain.Options;

using Microsoft.Extensions.DependencyInjection;

namespace FileDev.Infrastructres;

public static class FileDevExtenction
{
    public static IServiceCollection AddNotFileOptions(this IServiceCollection services,Action<FileDevConfigurationOptions> fileOptions)
    {
        services.Configure<FileDevConfigurationOptions>(fileOptions);
        services.AddOptions<FileDevConfigurationOptions>();
        return services;
    }
}
