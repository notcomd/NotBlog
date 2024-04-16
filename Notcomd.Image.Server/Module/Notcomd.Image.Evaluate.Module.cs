using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Module
{
    public class Notcomd_Image_Evaluate_Module:IDisposable
    {
        public long Image_Level { get; set; }
        public string Image_Type { get; set; }
        public string Image_Size { get; set; }
        public string Image_Ownership { get; set; }

        public Notcomd_Image_Evaluate_Module(long image_Level, string image_Type, string image_Size, string image_Ownership)
        {
            Image_Level = image_Level;
            Image_Type = image_Type;
            Image_Size = image_Size;
            Image_Ownership = image_Ownership;
        }

        public Notcomd_Image_Evaluate_Module() { }

        public void Dispose()
        {
            this.Dispose();
        }
    }
}
