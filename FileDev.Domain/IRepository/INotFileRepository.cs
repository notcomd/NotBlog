using DomainCommonst;

using FileDev.Domain.Entities;

namespace FileDev.Domain.IRepository;

public interface INotFileRepository : IRepository<NotFile>
{

    ValueTask AddWithNotFileAsync(NotFile notFile);

    ValueTask AddWithNotFileRangeAsync(IEnumerable<NotFile> notFiles);

    Task<NotFile> FileByFileAllAsync();

    Task<NotFile> FileByFileIdAsync(int id);

    ValueTask<NotFile> GetWithNotFileAsync(object getObject);

    ValueTask<NotFile> GetWithNotFileAsync(string getObject);

    ValueTask<NotFile> GetWithNotFileAsync(Guid getObject);

    ValueTask UpDataWithNotFileAsync(object findKey, Func<NotFile, Task> UpdataFunc);

    ValueTask UpDataWithNotFileAsync(string findKey, Func<NotFile, Task> UpdataFunc);

    ValueTask UpDataWithNotFileAsync(Guid findKey, Func<NotFile, Task> UpdataFunc);
}