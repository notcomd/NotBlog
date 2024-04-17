using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Module
{
    [Table("Notcomd_Document_data_Table")]
    public class Notcomd_Document_Module:IDisposable
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

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    this.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~Notcomd_Document_Module()
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
