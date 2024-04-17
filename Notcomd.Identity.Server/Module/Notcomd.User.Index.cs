using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    public class Notcomd_User_Index:IDisposable
    {
        public Notcomd_Role_Module Notcomd_Role_Module { get; set; }
        public Notcomd_User_Module Notcomd_User_Module { get; set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

       
    }
}
