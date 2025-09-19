using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileDev.Domain.Options;

using Microsoft.Extensions.DependencyInjection;

namespace FileDev.Infrastructres
{
    public static class FileExtenction
    {
        public static IServiceCollection AddNotFileOptions(this IServiceCollection services,Action<FileDevConfigurationOptions> fileOptions)
        {
            services.Configure<FileDevConfigurationOptions>(fileOptions);
            services.AddOptions<FileDevConfigurationOptions>();
            return services;
        }
    }
}
