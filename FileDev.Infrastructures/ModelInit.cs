using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DomainCommonst;



using Microsoft.Extensions.DependencyInjection;

using Notcomd.DomainCommand;

namespace FileDev.Infrastructres
{
    public class ModelInit : IModuleInitializer
    {
        public void Initialize(IServiceCollection service)
        {
            //service.AddScoped<IFileGroupRepository, FileGroupRepository>();
            //service.AddScoped<INotFileRepository, FileRepositoty>();
        }
    }
}
