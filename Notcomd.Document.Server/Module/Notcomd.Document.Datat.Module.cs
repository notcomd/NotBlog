



using System.ComponentModel.DataAnnotations.Schema;

namespace Notcomd.Document.Server.Module
{
    [Table("Notcomd_Document_Data_Module")]
    public class Notcomd_Document_Data_Module
    {
        public long Document_Upvote { get; set; }
        public long Document_Comment { get; set; }
        public long Document_Share { get; set; }
        public long Document_Collect { get; set; }
        public long Document_Examine { get; set; }


        public Notcomd_Document_Data_Module(long document_Upvote, long document_Comment, long document_Share, long document_Collect, long document_Examine)
        {
            Document_Upvote = document_Upvote;
            Document_Comment = document_Comment;
            Document_Share = document_Share;
            Document_Collect = document_Collect;
            Document_Examine = document_Examine;
        }
        //给EF Core 框架使用
        public Notcomd_Document_Data_Module() { }

        public void Set_Upvote()
        {
            this.Document_Upvote++;
        }

        public void Set_Comment()
        {
            this.Document_Comment++;
        }
        public void Set_Share()
        {
            this.Document_Share++;
        }
        public void Set_Collect()
        {
            this.Document_Collect++;
        }
        public void Set_Examine()
        {
            this.Document_Examine++;
        }
    }
}
