using Notcomd.Identity.Server.Module;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Mapper
{
    public interface INotcomd_Identity_Mapper
    {
        Task Send_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module);
        Task Get_Identity_UserAsync(object FindData);
        Task Updata_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module);
        Task Delete_Idnetity_UserAsync(Notcomd_User_Module notcomd_User_Module);
    }
}
