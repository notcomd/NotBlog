using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    [Table("User_Image_Table")]
    public class Notcomd_User_Image_Module:IDisposable
    {
        public string Identity_UserName { get; set; }
        public string? Identity_ImageUrl { get; set; }

        public void Dispose() { this.Dispose(); }

        public Notcomd_User_Image_Module(string identity_UserName, string? identity_ImageUrl)
        {
            Identity_UserName = identity_UserName;
            Identity_ImageUrl = identity_ImageUrl;
        }
        public Notcomd_User_Image_Module() { }
    }
}
