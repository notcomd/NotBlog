using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    [Table("UserImageTable")]
    public class Notcomd_User_Image_Module:IDisposable
    {
        [Key]
        public string Identity_UserName { get; set; }

        public List<string>? Identity_ImageUrl { get; set; }

        public Notcomd_User_Module notcomd_User_Modules { get; set; } = null!;

        public void Dispose() { this.Dispose(); }

        public Notcomd_User_Image_Module(string identity_UserName, List<string>? identity_ImageUrl,Notcomd_User_Module notcomd_User_Module)
        {
            Identity_UserName = identity_UserName;
            Identity_ImageUrl = identity_ImageUrl;
            notcomd_User_Modules = notcomd_User_Module;
        }
        public Notcomd_User_Image_Module() { }
    }
}
