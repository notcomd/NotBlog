using DomainCommon;

using FileDev.Domain.DomainEntities;

namespace FileDev.Domain.IRepository;

public interface INotFileRepository : IRepository<NotFileRepository>
{
    ValueTask<IEnumerable<NotFileRepository>?> GetNotFileRepositoryWithAllAsync();

    ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(Guid fileRepositoryGuid);

    ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(string fileRepositoryName);

    ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(object fileRepositoryObject);

    ValueTask AddNotFileRepositoryAsync(NotFileRepository repository);

    ValueTask RemoveNotFileRepositoryAsync(NotFileRepository repository);

    ValueTask UpdateNotFileRepositoryAsync(Guid fileRepositoryGuid, Func<NotFileRepository, Task> changeFunc);

    ValueTask UpdateNotFileRepositoryAsync(string fileRepositoryName, Func<NotFileRepository, Task> changeFunc);

    ValueTask UpdateNotFileRepositoryAsync(Object fileRepositoryObject, Func<NotFileRepository, Task> changeFunc);
}
