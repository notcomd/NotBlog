using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
