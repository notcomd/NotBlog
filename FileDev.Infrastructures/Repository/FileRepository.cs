using DomainCommonst;

using FileDev.Domain.DomainEntities;
using FileDev.Domain.FileDevException;
using FileDev.Domain.IRepository;
using FileDev.Infrastructres.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructres.Repository;

public class FileRepository : INotFileRepository
{


    private readonly FileDbContext _fileDbContext;

    private readonly ILogger<FileRepository> _logger;

    public IUnitOfWork UnitOfWork => _fileDbContext;

    private readonly INotDateTime _notDateTime;

    public FileRepository(FileDbContext fileDbContext, ILogger<FileRepository> logger, INotDateTime notDateTime)
    {
        _fileDbContext = fileDbContext;
        _logger = logger;
        _notDateTime = notDateTime;
    }



    public async ValueTask<Domain.DomainEntities.NotFileRepository?> GetNotFileRepositoryAsync(Guid fileRepositoryGuid)
    {
        try
        {
            var data = await _fileDbContext.NotFileRepository
                .Where(en => en.Id == fileRepositoryGuid)
                .FirstOrDefaultAsync();
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"");
            return null;
        }
    }

    public async ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(string fileRepositoryName)
    {
        try
        {
            if (string.IsNullOrEmpty(fileRepositoryName))
                throw new ArgumentNullException(fileRepositoryName);
            var data = await _fileDbContext.NotFileRepository.Where(en => string
             .Equals(en.NotFileRepositoryName, fileRepositoryName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefaultAsync();
            _logger.LogInformation($"[（*＾-＾*）]");
            return data;
        }
        catch (Exception ex)
        {
            throw new NotFileDevException(fileRepositoryName, ex);
        }
    }

    public async ValueTask<Domain.DomainEntities.NotFileRepository?> GetNotFileRepositoryAsync(object fileRepositoryObject)
    {
        try
        {
            if (object.ReferenceEquals(fileRepositoryObject, null))
            {
                throw new NotImplementedException();
            }
            else if (fileRepositoryObject is string find)
            {
                var data = await _fileDbContext.NotFileRepository
                    .Where(en => en.NotFileRepositoryName == find)
                    .FirstOrDefaultAsync();
                return data;
            }
            else if (fileRepositoryObject is Guid id)
            {
                var data = await _fileDbContext.NotFileRepository
                    .Where(en => en.NotFileRepositoryName.Equals(id))
                    .FirstOrDefaultAsync();
                return data;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"{ex.Message}");
            return null;
        }
    }

    public async ValueTask AddNotFileRepositoryAsync(Domain.DomainEntities.NotFileRepository repository)
    {
        await _fileDbContext.NotFileRepository.AddAsync(repository);
    }

    public ValueTask RemoveNotFileRepositoryAsync(Domain.DomainEntities.NotFileRepository repository)
    {
        throw new NotImplementedException();
    }

    public async ValueTask UpdateNotFileRepositoryAsync(Guid fileRepositoryGuid,
        Func<Domain.DomainEntities.NotFileRepository, Task> changeFunc)
    {
        try
        {
            var data = await GetNotFileRepositoryAsync(fileRepositoryGuid);
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            await changeFunc(data);
            return;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return;
        }
        finally
        {

        }
    }

    public async ValueTask UpdateNotFileRepositoryAsync(string fileRepositoryName,
        Func<Domain.DomainEntities.NotFileRepository, Task> changeFunc)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileRepositoryName))
                throw new ArgumentNullException(nameof(fileRepositoryName));
            var data = await GetNotFileRepositoryAsync(fileRepositoryName);
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            await changeFunc(data);
            return;
        }
        catch (Exception ex)
        {
            throw new NotFileDevException(fileRepositoryName, ex);
        }
    }

    public async ValueTask UpdateNotFileRepositoryAsync(object fileRepositoryObject,
        Func<Domain.DomainEntities.NotFileRepository, Task> changeFunc)
    {
        try
        {
            if (fileRepositoryObject is null)
                throw new ArgumentException(nameof(fileRepositoryObject));
            object data;
            if (fileRepositoryObject is Guid fileguid)
            {
                data = await GetNotFileRepositoryAsync(fileguid);
            }
            else if (fileRepositoryObject is string filestring)
            {
                data = await GetNotFileRepositoryAsync(filestring);
            }
            else
            {
                throw new NotFileDevException("错误的值");
            }
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            await changeFunc(data as NotFileRepository);
            _logger.LogInformation($"[（*＾-＾*）]{_notDateTime.UtcNow}");
            return;
        }
        catch (Exception ex)
        {
            throw new NotFileDevException("", ex);
        }
        finally
        {
            _logger.LogWarning($"[（*＾-＾*）]");
        }
    }

    public async ValueTask<IEnumerable<NotFileRepository>> GetNotFileRepositoryWithAllAsync()
    {
        return await _fileDbContext.NotFileRepository.Include(en => en.FileGroups).ToListAsync();
    }
}
