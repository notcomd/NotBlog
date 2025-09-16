using DomainCommonst;

using FileDev.Domain.IRepository;
using FileDev.Infrastructres.DbContext;

namespace FileDev.Infrastructres.Repository
{
    public class FileGroupRepository : IFileGroupRepository
    {
        private readonly FileDbContext _fileDbContext;
        private readonly INotDateTime _notDateTime;

        public IUnitOfWork UnitOfWork => _fileDbContext;

        public FileGroupRepository(FileDbContext fileDbContext, INotDateTime notDateTime)
        {
            _fileDbContext = fileDbContext;
            _notDateTime = notDateTime;
        }





    }
}
