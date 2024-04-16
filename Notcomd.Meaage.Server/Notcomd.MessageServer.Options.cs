using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Notcomd.Meaage.Server
{
    public static class Notcomd_MessageServer_Options
    {
        public static IServiceCollection AddMessageServer(this IServiceCollection services)
        {

            return services;
        }

        public static IServiceCollection AddMessageServer(this IServiceCollection services, IConfiguration configural)
        {

            return services;
        }
    }
}
