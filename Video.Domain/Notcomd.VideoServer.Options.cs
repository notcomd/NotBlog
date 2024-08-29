using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.Video.Server
{
    public static class Notcomd_VideoServer_Options
    {
        public static IServiceCollection AddVideoServer(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddVideoServer(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
