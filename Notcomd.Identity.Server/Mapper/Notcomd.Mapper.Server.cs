using Notcomd.Identity.Server.Module;

namespace Notcomd.Identity.Server.Mapper
{
    public class Notcomd_Mapper_Server:INotcomd_Mapper_Server
    {
        public Task<IQueryable<List<Notcomd_User_Index>>> GetNotcomd_User_ModulesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Notcomd_User_Index> GetNotcomd_User_IndexAsync(object userdata)
        {
            throw new NotImplementedException();
        }

        public Task SetNotcomd_User_ModuleAsync(Notcomd_User_Index notcomd_User_Index)
        {
            throw new NotImplementedException();
        }

        public Task SetNotcomd_User_Image_ModuleAsync()
        {
            throw new NotImplementedException();
        }
    }
}
