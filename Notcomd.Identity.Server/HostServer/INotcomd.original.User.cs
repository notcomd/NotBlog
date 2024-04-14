using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Notcomd.Identity.Server.HostServer
{
    public interface INotcomd_Original_User
    {
        Task WorkAsync(CancellationToken cancellationToken);
    }
}
