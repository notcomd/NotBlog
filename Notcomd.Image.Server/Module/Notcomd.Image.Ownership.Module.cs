using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Module
{
    public class Notcomd_Image_Ownership_Module:IDisposable
    {
        public string Image_Ownership_UserName { get; set; } = null!;
        public string Image_User_HeadPort { get; set; } = null!;
        public bool Image_Privati { get; set; } = true;
        public ICollection<Notcomd_Image_Module> notcomd_Image_Modules { get; set; }
        public Notcomd_Image_Ownership_Module() { }
        public Notcomd_Image_Ownership_Module(string image_Ownership_UserName, string image_User_HeadPort)
        {
            Image_Ownership_UserName = image_Ownership_UserName;
            Image_User_HeadPort = image_User_HeadPort;
        }

        public void Dispose()
        {
            this.Dispose();
        }
    }
}
