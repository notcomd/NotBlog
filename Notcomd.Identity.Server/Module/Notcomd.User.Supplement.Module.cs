using Amazon.Runtime.Internal.Util;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.Module
{
    
    public class Notcomd_User_Supplement_Module:IDisposable
    {
        [Key]
        public string? WeChat_Numble {get;set;}
        public bool UserLin_Status { get; set; }
        public List<string> UserTag { get; set; }
        public string? UserRemake { get; set; }
        public ICollection<Notcomd_User_Module> Notcomd_User_Modules { get; set; }

        public Notcomd_User_Supplement_Module() { }
        public Notcomd_User_Supplement_Module(string? weChat_Numble, bool userLin_Status, List<string> userTag, string? userRemake)
        {
            WeChat_Numble = weChat_Numble;
            UserLin_Status = userLin_Status;
            UserTag = userTag;
            UserRemake = userRemake;
        }

        private void SentUserLin_Status(bool Status)
        {
            this.UserLin_Status = Status;
        }

        public void Dispose()
        {
            try
            {
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            } 
        }
    }
}
