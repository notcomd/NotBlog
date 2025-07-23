using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.IRepository
{
    public interface IAuthor2Repository:IRepository<Author2>
    {

        Task<Author2?> FindOneByAuthorAsync(Guid guid);
        
        Task<Author2?> FindOneByAuthorAsync(string authorName);

        Task AddOneByAuthorAsync(Author2 author);

        
    }
}
