using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Module
{
    public class Notcomd_Image_Index_Module:IDisposable
    {
        
        public Notcomd_Image_Evaluate_Module notcomd_Image_Evaluate_Module { get; set; }
        public Notcomd_Image_Module notcomd_Image_Module { get; set; }
        public Notcomd_Image_Ownership_Module notcomd_Image_Ownership_Module { get; set; }

        public Notcomd_Image_Index_Module() { }

        public Notcomd_Image_Index_Module (Notcomd_Image_Evaluate_Module notcomd_Image_Evaluate_Module, Notcomd_Image_Module notcomd_Image_Module, Notcomd_Image_Ownership_Module notcomd_Image_Ownership_Module)
        {
            this.notcomd_Image_Evaluate_Module = notcomd_Image_Evaluate_Module;
            this.notcomd_Image_Module = notcomd_Image_Module;
            this.notcomd_Image_Ownership_Module = notcomd_Image_Ownership_Module;
        }

        public void Dispose()
        {
            this.Dispose();
           // throw new NotImplementedException();
        }
    }
}
