using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Module
{
    [Table("Notcomd_Document_data_Table")]
    public class Notcomd_Document_Module
    {
        [Column(TypeName="string")]
        public string Document_UID { get; set; }
        [Column(TypeName="string")]
        public string Document_Title { get; set; }
        [Column(TypeName="string")]
        public string Document_Cover { get; set; }
        [Column(TypeName="string")]
        public string Document_CreateDate { get; set; }
        public string Document_Version { get; set; }
        [Column(TypeName ="text")]
        public object Document_Data { get; set; }
        
    }
}
