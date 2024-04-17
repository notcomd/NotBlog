using Microsoft.AspNetCore.Identity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    public class Notcomd_User_Module:IdentityUser<long>,IDisposable
    {
        public Notcomd_User_Image_Module? Notcomd_User_Image_Module { get; set; } = new Notcomd_User_Image_Module();
        public Notcomd_User_Supplement_Module? Notcomd_User_Supplement_Module { get; set; } = new Notcomd_User_Supplement_Module();
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
