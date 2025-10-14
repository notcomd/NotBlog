using DomainCommon;
using FileDev.Domain.DomainEntities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructres.DbContext; // 修正拼写错误
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileDev.Infrastructres.Repository // 修正拼写错误
{
    public class FileRepository : INotFileRepository
    {
        private readonly FileDbContext _fileDbContext;
        private readonly ILogger<FileRepository> _logger;
        private readonly INotDateTime _notDateTime;

        public IUnitOfWork UnitOfWork => _fileDbContext;

        public FileRepository(FileDbContext fileDbContext, ILogger<FileRepository> logger, INotDateTime notDateTime)
        {
            _fileDbContext = fileDbContext;
            _logger = logger;
            _notDateTime = notDateTime;
        }

        // 提取通用查询逻辑，减少代码重复
        private async ValueTask<NotFileRepository?> QueryNotFileRepositoryAsync(Func<IQueryable<NotFileRepository>, IQueryable<NotFileRepository>> query)
        {
            try
            {
                return await query(_fileDbContext.NotFileRepository).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "查询 NotFileRepository 时发生错误");
                return null;
            }
        }

        public async ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(Guid fileRepositoryGuid)
        {
            return await QueryNotFileRepositoryAsync(query => query.Where(en => en.Id == fileRepositoryGuid));
        }

        public async ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(string fileRepositoryName)
        {
            if (string.IsNullOrEmpty(fileRepositoryName))
                throw new ArgumentNullException(nameof(fileRepositoryName), "文件仓库名称不能为空");

            try
            {
                var data = await _fileDbContext.NotFileRepository
                    .FirstOrDefaultAsync(en => string.Equals(
                        en.NotFileRepositoryName,
                        fileRepositoryName,
                        StringComparison.OrdinalIgnoreCase));

                _logger.LogInformation($"成功查询 NotFileRepository，名称: {fileRepositoryName}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"查询 NotFileRepository 时发生错误，名称: {fileRepositoryName}");
                return null;
            }
        }

        public async ValueTask<NotFileRepository?> GetNotFileRepositoryAsync(object fileRepositoryObject)
        {
            if (fileRepositoryObject is null)
                throw new ArgumentNullException(nameof(fileRepositoryObject), "查询参数不能为 null");

            try
            {
                if (fileRepositoryObject is string name)
                {
                    return await GetNotFileRepositoryAsync(name);
                }
                else if (fileRepositoryObject is Guid id)
                {
                    return await GetNotFileRepositoryAsync(id);
                }
                else
                {
                    throw new ArgumentException("不支持的查询参数类型", nameof(fileRepositoryObject));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"使用对象参数查询 NotFileRepository 时发生错误，参数: {fileRepositoryObject}");
                return null;
            }
        }

        public async ValueTask AddNotFileRepositoryAsync(NotFileRepository repository)
        {
            await _fileDbContext.NotFileRepository.AddAsync(repository);
        }

        public ValueTask RemoveNotFileRepositoryAsync(NotFileRepository repository)
        {
            throw new NotImplementedException("删除 NotFileRepository 方法尚未实现");
        }

        // 提取通用更新逻辑，减少代码重复
        /// <summary>
        /// 核心更新文件仓库的异步方法。该方法用于根据传入的获取数据函数查找文件仓库，
        /// 若找到则保存更改到数据库，若未找到则抛出异常。
        /// </summary>
        /// <param name="getDataFunc">用于异步获取 NotFileRepository 对象的函数，若未找到则返回 null</param>
        /// <param name="identifier">文件仓库的标识值，用于日志记录和错误提示</param>
        /// <param name="identifierType">标识的类型，如 "ID" 或 "名称"，用于日志记录和错误提示</param>
        private async ValueTask UpdateNotFileRepositoryCoreAsync(Func<Task<NotFileRepository?>> getDataFunc, string identifier, string identifierType)
        {
            var data = await getDataFunc();
            if (data is null)
                throw new ArgumentNullException(nameof(data), $"未找到 {identifierType} 为 {identifier} 的文件仓库");

            try
            {
                //await changeFunc(data);
                await _fileDbContext.SaveChangesAsync();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新 NotFileRepository 时发生错误，{identifierType}: {identifier}");
                throw;
            }
        }

        public async ValueTask UpdateNotFileRepositoryAsync(Guid fileRepositoryGuid, Func<NotFileRepository, Task> changeFunc)
        {
            await UpdateNotFileRepositoryCoreAsync(
                async () => await GetNotFileRepositoryAsync(fileRepositoryGuid),
                fileRepositoryGuid.ToString(),
                "ID");
        }

        public async ValueTask UpdateNotFileRepositoryAsync(string fileRepositoryName, Func<NotFileRepository, Task> changeFunc)
        {
            if (string.IsNullOrWhiteSpace(fileRepositoryName))
                throw new ArgumentNullException(nameof(fileRepositoryName), "文件仓库名称不能为空");

            await UpdateNotFileRepositoryCoreAsync(
                async () => await GetNotFileRepositoryAsync(fileRepositoryName),
                fileRepositoryName,
                "名称");
        }

        public ValueTask<IEnumerable<NotFileRepository>?> GetNotFileRepositoryWithAllAsync()
        {
            throw new NotImplementedException();
        }

        public ValueTask UpdateNotFileRepositoryAsync(object fileRepositoryObject, Func<NotFileRepository, Task> changeFunc)
        {
            throw new NotImplementedException();
        }
    }
}