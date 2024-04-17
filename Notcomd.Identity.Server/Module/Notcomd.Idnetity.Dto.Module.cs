using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
   public record Notcomd_Login_Module(string Email,string Password) : IDisposable
    {
        public void Dispose() { GC.SuppressFinalize(this); }
    }

    public record Notcomd_Signup_Module(string Email,string passowrd,string ReCaptcha) : IDisposable
    {
        public void Dispose() { GC.SuppressFinalize(this); }
    }
}
