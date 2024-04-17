using Microsoft.AspNetCore.Mvc;

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
        Task<ActionResult<string>> Send_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module);
        Task<ActionResult<Notcomd_User_Module>> Get_Identity_UserAsync(object FindData);
        Task Updata_Identity_UserAsync(Notcomd_User_Module notcomd_User_Module);
        Task Delete_Idnetity_UserAsync(Notcomd_User_Module notcomd_User_Module);
    }
}
