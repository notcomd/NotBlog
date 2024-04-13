using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Net.EmailServer
{
    public  interface INotcomd_Email_Server
    {
        Task SendEmailAsync(string UserEmailAdddress,params string[] args);
    }
}
