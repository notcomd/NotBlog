using Notcomd.Identity.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Mapper
{
    public interface INotcomd_Mapper_Server
    {
        Task<IQueryable<List<Notcomd_User_Index>>> GetNotcomd_User_ModulesAsync();

        Task<Notcomd_User_Index> GetNotcomd_User_IndexAsync(object userdata);

        Task SetNotcomd_User_ModuleAsync(Notcomd_User_Index notcomd_User_Index);

        Task SetNotcomd_User_Image_ModuleAsync();
    }
}
