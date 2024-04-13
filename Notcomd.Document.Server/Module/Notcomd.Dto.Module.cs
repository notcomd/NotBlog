using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Document.Server.Module
{
    public record Notcomd_Document_Dto_Comment(string documentusername ,object commentdata) : IDisposable {
        public void Dispose()
        {
            this.Dispose();
        }
    }

   public record Notcomd_Document_Dto_Data() : IDisposable {
        public void Dispose()
        {
            this.Dispose();
        }
    }
    
    public record Notcomd_Document_Dto_Module() : IDisposable {
        public void Dispose()
        {
            this.Dispose();
        }
    }

    public record Notcomd_Document_Dto_Ownership():IDisposable {
        public void Dispose()
        {
            this.Dispose();
        }
    }
}
