



using System.ComponentModel.DataAnnotations.Schema;

namespace Notcomd.Document.Server.Module
{
    /// <summary>
    /// 观看实体模型
    /// </summary>
    [Table("Notcomd_Document_Data_Module")]
    public class Notcomd_Document_Data_Module:IDisposable
    {
        public long Document_Upvote { get; set; } = 0;
        public long Document_Comment { get; set; } = 0;
        public long Document_Share { get; set; } = 0;
        public long Document_Collect { get; set; } = 0;
        public long Document_Examine { get; set; } = 0;
        //这个不需要
        public ICollection<Notcomd_Document_Module> Notcomd_Document_Modules { get; set; }

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

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~Notcomd_Document_Data_Module()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
