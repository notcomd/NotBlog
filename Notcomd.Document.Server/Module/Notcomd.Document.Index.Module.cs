using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Notcomd.Document.Server.Module
{
    /// <summary>
    /// 总体类不在数据库中参与实体类型生成。主要用返回类型
    /// </summary>
    public class Notcomd_Document_Index_Module:IDisposable
    {
        public Notcomd_Document_Module Notcomd_Document_Module { get; set; }
        public List<Notcomd_Document_Comment_Module> notcomd_Document_Comment_Modules { get; set; }
        public Notcomd_Document_Ownership_Module Notcomd_Document_Ownership_Module { get; set; }
        public Notcomd_Document_Data_Module Notcomd_Document_Data_Module { get; set; }

        public void Dispose()
        {
            this.Dispose();
            //throw new NotImplementedException();   
        }
    }
}
