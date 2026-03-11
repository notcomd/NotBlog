using FileDev.Domain.Entities;
using FileDev.Domain.IRepository;
using FileDev.Domain.SeedWork;
using FileDev.Infrastructure.EntityFramework;

namespace FileDev.Infrastructure.Repository;

public class NotFileGroupRepository(NotFileDbContext notFileDbContext) : INotFileGroupRepository
{
    
    public IUnitOfWork UnitOfWork => notFileDbContext;


    public Task InsetNotFileGroupAsync(NotFileGroup notFileGroup)
    {
        throw new NotImplementedException();
    }

    public Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByTypeAsync(FileType fileType)
    {
        throw new NotImplementedException();
    }
}