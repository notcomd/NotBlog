using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server
{
    public static class Notcomd_Image_Server_Options
    {
        public static IServiceCollection AddImageServer(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddImageServer(this IServiceCollection services,IConfiguration configuration)
        {
            return services;
        }
    }
}
