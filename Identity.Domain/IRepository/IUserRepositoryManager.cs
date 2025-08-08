using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.IRepository
{
    public interface IUserRepositoryManager
    {

        ValueTask DeleteByUser(Guid UserGuid);


    }
}
