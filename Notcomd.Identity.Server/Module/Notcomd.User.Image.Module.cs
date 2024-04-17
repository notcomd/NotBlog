using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Notcomd.Identity.Server.Module
{
    [Table("UserImageTable")]
    public class Notcomd_User_Image_Module:IDisposable
    {
        [Key]
        public string Identity_UserName { get; set; }

        public List<string>? Identity_ImageUrl { get; set; }

        public ICollection<Notcomd_User_Module> Notcomd_User_Modules { get; set; }

        public void Dispose() { GC.SuppressFinalize(this); }

        public Notcomd_User_Image_Module(string identity_UserName, List<string>? identity_ImageUrl,Notcomd_User_Module notcomd_User_Module)
        {
            Identity_UserName = identity_UserName;
            Identity_ImageUrl = identity_ImageUrl;
        }
        public Notcomd_User_Image_Module() { }

    }
}
