using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Module
{
    public class Notcomd_Image_Ownership_Module:IDisposable
    {
        [Key]
        public string Image_Ownership_UserName { get; set; } = null!;
        public string Image_User_HeadPort { get; set; } = null!;
        public bool Image_Privati { get; set; } = true;
<<<<<<< HEAD
        public ICollection<Notcomd_Image_Module> Notcomd_Image_Module { get; set; }
=======
        public ICollection<Notcomd_Image_Module> notcomd_Image_Modules { get; set; }
>>>>>>> 5e06be5c8a45237ed4ab9101f8316484780ae68c
        public Notcomd_Image_Ownership_Module() { }
        public Notcomd_Image_Ownership_Module(string image_Ownership_UserName, string image_User_HeadPort,bool image_private)
        {
            Image_Ownership_UserName = image_Ownership_UserName;
            Image_User_HeadPort = image_User_HeadPort;
            Image_Privati = image_private;
        }

        public void Dispose()
        {
            this.Dispose();
        }
    }
}
