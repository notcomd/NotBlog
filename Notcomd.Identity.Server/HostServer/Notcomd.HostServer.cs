using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Notcomd.Identity.Server.Mapper;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.HostServer
{
    public class Notcomd_HostServer:BackgroundService
    {
        private readonly ILogger<INotcomd_Original_User> _logger;
        private readonly IServiceScope _serviceScope;

        public Notcomd_HostServer(ILogger<INotcomd_Original_User> logger, IServiceScope serviceScope)
        {
            _logger = logger;
            _serviceScope = serviceScope;
        }
        public override void Dispose()
        {
            _serviceScope.Dispose();
            base.Dispose();
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var BackService = _serviceScope.ServiceProvider.GetRequiredService<INotcomd_Original_User>();
                _serviceScope.ServiceProvider.GetRequiredService<Notcomd_Identity_DbContext>();
                await BackService.WorkAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"INFO:Message[ X﹏X 服务错误！详细信息：{ex} | 时间:{DateTime.UtcNow}]");         
            }
        }
    }
}
