using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.IRepository
{
    public interface IUserRepositoryManager
    {

        ValueTask<IEnumerable<User?>> FindByUserAsync(Guid[] guids);


        ValueTask DeleteByUser(Guid UserGuid);


    }
}
