
using DomainCommonst;

using FileDev.Domain.IRepository;
using FileDev.Infrastructres.DbContext;

using NotFile = FileDev.Domain.Entities.NotFile;

namespace FileDev.Infrastructres.Repository;

public class FileRepositoty : INotFileRepository
{

    private readonly FileDbContext _fileDbContext;

    public IUnitOfWork  unitOfWork => _fileDbContext;


    public async Task<NotFile> FileByFileAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<NotFile> FileByFileIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public ValueTask AddWithNotFileAsync(NotFile notFile)
    {
        throw new NotImplementedException();
    }

    public ValueTask AddWithNotFileRangeAsync(IEnumerable<NotFile> notFiles)
    {
        throw new NotImplementedException();
    }

    public ValueTask<NotFile> GetWithNotFileAsync(object getObject)
    {
        throw new NotImplementedException();
    }

    public ValueTask<NotFile> GetWithNotFileAsync(string getObject)
    {
        throw new NotImplementedException();
    }

    public ValueTask<NotFile> GetWithNotFileAsync(Guid getObject)
    {
        throw new NotImplementedException();
    }

    public ValueTask UpDataWithNotFileAsync(object findKey, Func<NotFile, Task> UpdataFunc)
    {
        throw new NotImplementedException();
    }

    public ValueTask UpDataWithNotFileAsync(string findKey, Func<NotFile, Task> UpdataFunc)
    {
        throw new NotImplementedException();
    }

    public ValueTask UpDataWithNotFileAsync(Guid findKey, Func<NotFile, Task> UpdataFunc)
    {
        throw new NotImplementedException();
    }

    public IUnitOfWork UnitOfWork => throw new NotImplementedException();
}