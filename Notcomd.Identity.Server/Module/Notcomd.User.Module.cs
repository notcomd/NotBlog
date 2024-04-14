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
        public string WeChat_Numble { get; set; }

        public  Notcomd_User_Image_Module? notcomd_User_Image_Module { get; set; }

        public bool Status { get; set; }

        public string Ponit { get; set; }

        public void Dispose() { this.Dispose(); }

        public Notcomd_User_Module() { }

        public Notcomd_User_Module(string weChat_Numble, bool status, string ponit,Notcomd_User_Image_Module notcomd_User_Image_Module)
        {
            WeChat_Numble = weChat_Numble;
            Status = status;
            Ponit = ponit;
            this.notcomd_User_Image_Module = notcomd_User_Image_Module;
        }
    }
}
