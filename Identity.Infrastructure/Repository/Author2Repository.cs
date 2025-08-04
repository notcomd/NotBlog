using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.Repository
{
    public class Author2Repository:IAuthor2Repository
    {

        private readonly IdentityDbContext _identityDbContext;
        private readonly ILogger<Author2Repository> _logger;
        public IUnitOfWork UnitOfWork => _identityDbContext;

        public Author2Repository(IdentityDbContext identityDbContext, ILogger<Author2Repository> logger)
        {
            _identityDbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
            _logger = logger;
            //_logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Author2Repository>();
        }

        public async Task<Author2?> FindOneByAuthorAsync(Guid authorGuid)
        {
            return await _identityDbContext.Author2s.Where(en => en.Author2Guid == authorGuid)
                .SingleOrDefaultAsync();
        }

        public Task<Author2?> FindOneByAuthorAsync(string authorName)
        {
            throw new NotImplementedException();
        }

        public Task AddOneByAuthorAsync(Author2 author)
        {
            throw new NotImplementedException();
        }

       
    }
}
