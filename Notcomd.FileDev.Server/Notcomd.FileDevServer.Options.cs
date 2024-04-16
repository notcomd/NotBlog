using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.FileDev.Server
{
    public static class Notcomd_FileDevServer_Options
    {
        public static IServiceCollection AddFileDevServer( this IServiceCollection services)
        {
            return services;
        }
        public static IServiceCollection AddFileDevServer(this IServiceCollection services, IConfiguration configuration)
        {
            return services;
        }
    }
}
