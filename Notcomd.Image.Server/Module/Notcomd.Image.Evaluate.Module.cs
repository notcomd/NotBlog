using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Key]
        public string Image_Ownership { get; set; }
<<<<<<< HEAD

        public ICollection<Notcomd_Image_Module> Notcomd_Image_Module { get; set; }

        public ICollection<Notcomd_Image_Module> notcomd_Image_Modules { get; set; }

=======
<<<<<<< HEAD
        public ICollection<Notcomd_Image_Module> Notcomd_Image_Module { get; set; }
=======
        public ICollection<Notcomd_Image_Module> notcomd_Image_Modules { get; set; }
>>>>>>> 5e06be5c8a45237ed4ab9101f8316484780ae68c
>>>>>>> 1bc8c836d3b77d793591b5ce2d586d5d7e18079f
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
