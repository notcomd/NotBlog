using Microsoft.Extensions.Logging;

using Notcomd.Identity.Server.Module;

namespace Notcomd.Identity.Server.Mapper
{
    public class Notcomd_Mapper_Server:INotcomd_Identity_Mapper
    {
        private readonly ILogger<INotcomd_Identity_Mapper> _logger;

        public Notcomd_Mapper_Server(ILogger<INotcomd_Identity_Mapper> logger)
        {
            _logger = logger;
        }

        public async Task Send_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module)
        {
#if DEBUG
            _logger.LogDebug($"DateTime:{DateTime.UtcNow}|INFO:Message[现在是调试]");
           using(var Dbcontext=new Notcomd_Identity_DbContext())
            {
                await Dbcontext.AddAsync(notcomd_User_Module);
                await Dbcontext.SaveChangesAsync();
            }
#endif
            _logger.LogInformation($"DateTime:{DateTime.UtcNow}|INFO:Message[现在是运行]");
            using (var Dbcontext=new Notcomd_Identity_DbContext())
            {
                await Dbcontext.AddAsync(notcomd_User_Module);
                await Dbcontext.SaveChangesAsync();
            }
        }

        public Task Get_Identity_UserAsync(object FindData)
        {
            throw new NotImplementedException();
        }

        public Task Updata_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module)
        {
            throw new NotImplementedException();
        }

        public Task Delete_Idnetity_UserAsync(Notcomd_User_Module notcomd_User_Module)
        {
            throw new NotImplementedException();
        }
    }
}
