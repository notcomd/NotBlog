using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Module
{
    [Table("Notcomd_Document_Ownership_Table")]
    public class Notcomd_Document_Ownership_Module
    {
        public string Document_Ownership { get; set; }
        public List<string> Document_Tag { get; set; }
        public bool Document_Publicity { get; set; }
    }
}
