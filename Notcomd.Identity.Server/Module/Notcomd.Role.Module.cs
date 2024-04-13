using Microsoft.AspNetCore.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    public class Notcomd_Role_Module:IdentityRole<long>,IDisposable
    {
        public void Dispose()
        {
            this.Dispose();
        }
    }
}
