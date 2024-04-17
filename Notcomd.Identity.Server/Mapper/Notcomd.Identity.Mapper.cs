using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Notcomd.Identity.Server.Module;

namespace Notcomd.Identity.Server.Mapper
{
    public class Notcomd_Identity_Mapper:INotcomd_Identity_Mapper
    {
        private readonly ILogger<INotcomd_Identity_Mapper> _logger;

        public Notcomd_Identity_Mapper(ILogger<INotcomd_Identity_Mapper> logger)
        {
            _logger = logger;
        }

        public async Task<ActionResult<string>> Send_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module)
        {
#if DEBUG
            _logger.LogDebug($"DateTime:{DateTime.UtcNow}|INFO:Message[现在是调试]");
           using(var Dbcontext=new Notcomd_Identity_DbContext())
            {
                await Dbcontext.AddAsync(notcomd_User_Module);
                await Dbcontext.SaveChangesAsync();
                return new ActionResult<string>(string.Empty);
            }
#endif
            _logger.LogInformation($"DateTime:{DateTime.UtcNow}|INFO:Message[现在是运行]");
            using (var Dbcontext=new Notcomd_Identity_DbContext())
            {
                await Dbcontext.AddAsync(notcomd_User_Module);
                await Dbcontext.SaveChangesAsync();
                return new ActionResult<string>(string.Empty);
            }
        }

        public async Task<ActionResult<Notcomd_User_Module>> Get_Identity_UserAsync(object FindData)
        {
#if DEBUG
            using(var Dbcontext=new Notcomd_Identity_DbContext())
            {

                return new ActionResult<Notcomd_User_Module>(new Notcomd_User_Module());
            }
#endif
            using(var Dbcontext=new Notcomd_Identity_DbContext())
            {

                return new ActionResult<Notcomd_User_Module>(new Notcomd_User_Module());
            }
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
