using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Module
{
   //评论模型
    public class Notcomd_Document_Comment_Module:IDisposable
    {
        [Key]
        public string Document_UserName { get; set; }
        public object Document_Comment { get; set; }
        public object? Document_Comment_Quote { get; set; }
        public ICollection<Notcomd_Document_Module> Notcomd_Document_Modules { get; set; }
        public Notcomd_Document_Comment_Module(string document_UserName, object document_Comment, object? document_Comment_Quote)
        {
            Document_UserName = document_UserName;
            Document_Comment = document_Comment;
            Document_Comment_Quote = document_Comment_Quote;
        }

        public Notcomd_Document_Comment_Module() { }

        public void Dispose()
        {
            this.MemberwiseClone();
            GC.SuppressFinalize(this);
        }

        
    }
}
