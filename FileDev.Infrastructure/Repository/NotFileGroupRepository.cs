using FileDev.Domain.Entities;
using FileDev.Domain.Exception;
using FileDev.Domain.IRepository;
using FileDev.Domain.SeedWork;
using FileDev.Infrastructure.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace FileDev.Infrastructure.Repository;

public class NotFileGroupRepository(NotFileDbContext notFileDbContext) : INotFileGroupRepository
{
    private readonly NotFileDbContext _notFileDbContext = notFileDbContext
                                                          ?? throw new ArgumentNullException(nameof(notFileDbContext));

    public IUnitOfWork UnitOfWork => notFileDbContext;


    public async Task InsertNotFileGroupAsync(NotFileGroup notFileGroup)
    {
        if(notFileGroup is null)
            throw new NotFileException("NotFileGroup is null");
        await _notFileDbContext.NotFileGroups.AddAsync(notFileGroup);
    }

    public async Task<NotFileGroup> GetNotFileGroupByIdAsync(Guid notFileGroupId)
    {
        if (Guid.Empty == notFileGroupId)
            throw new NotFileException("notFileGroupId is null");
        var data = await _notFileDbContext
            .NotFileGroups
            .FirstOrDefaultAsync(x =>
                x.NotFileGroupId.Equals(notFileGroupId) || x.IsDeleted ||
                (x.FileIdentity.Equals(FileIdentity.FilePrivate)));

        return data ?? throw new NotFileException("NotFileGroup is null");
    }

    public async Task<IEnumerable<NotFileGroup>> GetAllNotFileGroupsAsync()
    {
        var data = await _notFileDbContext
            .NotFileGroups
            .ToListAsync();
        return data.AsEnumerable();
    }

    public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new NotFileException("userId is null");
        var data = await _notFileDbContext.NotFileGroups
            .Where(x => x.UserId == userId)
            .ToListAsync();
        return data.AsEnumerable();
    }

    public async Task<IEnumerable<NotFileGroup>> GetPublicNotFileGroupsAsync()
    {
        return await _notFileDbContext.NotFileGroups
            .Where(x => x.FileIdentity == FileIdentity.FilePublic)
            .ToListAsync();
    }

    // public async Task<IEnumerable<NotFileGroup>> GetNotFileGroupsByTypeAsync(FileType fileType)
    // {
        
    //     return await _notFileDbContext.NotFileGroups
    //         .Where(x => x.FileType == fileType)
    //         .ToListAsync();
    // }

    public async Task<NotFileGroup?> GetNotFileGroupByNameAsync(string fileGroupName)
    {
        return await _notFileDbContext.NotFileGroups
            .FirstOrDefaultAsync(x => x.FileGroupName == fileGroupName);
    }

    public async Task<NotFileGroup?> UpdateNotFileGroupAsync(NotFileGroup notFileGroup)
    {
        return await _notFileDbContext.NotFileGroups
            .FirstOrDefaultAsync(x => x.NotFileGroupId == notFileGroup.NotFileGroupId);
    }

    public async Task DeleteNotFileGroupAsync(Guid notFileGroupId)
    {
        var data = await _notFileDbContext.NotFileGroups
            .FirstOrDefaultAsync(x => x.NotFileGroupId == notFileGroupId);
        if (data is null)
            throw new NotFileException("NotFileGroup is null");
        _notFileDbContext.NotFileGroups.Remove(data);
    }
}