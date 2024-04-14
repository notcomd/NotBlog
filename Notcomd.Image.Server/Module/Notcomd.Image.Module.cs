using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Image.Server.Module
{
    [Table("Notcomd_Image_Table")]
    public class Notcomd_Image_Module
    {
        public List<string> ImageTage { get; set; }
        public string Image_Name { get; set; }
        public string Image_Url { get; set; }
       

        [Column(TypeName ="Text")]
        public string Image_Remake { get; set; }
        public DateTime Image_CreateTime { get; set; }=DateTime.UtcNow;
        
    }
}
