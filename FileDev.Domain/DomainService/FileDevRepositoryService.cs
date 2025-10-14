// 修正拼写错误
using DomainCommon;

using FileDev.Domain.DomainEntities;
using FileDev.Domain.IRepository;

using Microsoft.Extensions.Logging;

namespace FileDev.Domain.DomainService;

public class FileDevRepositoryService
{
    private readonly INotFileRepository _notFileRepository;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<FileDevRepositoryService> _logger;

    public FileDevRepositoryService(INotFileRepository notFileRepository, INotDateTime notDateTime,
        ILogger<FileDevRepositoryService> logger)
    {
        _notFileRepository = notFileRepository ?? throw new ArgumentNullException(nameof(notFileRepository));
        _notDateTime = notDateTime ?? throw new ArgumentNullException(nameof(notDateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask AddNotFileGroupAsync(FileGroup fileGroup, CancellationToken cancellationToken)
    {
        if (fileGroup is null)
        {
            ArgumentNullException.ThrowIfNull(fileGroup);
        }

        try
        {
            var data = await _notFileRepository.GetNotFileRepositoryAsync(fileGroup.FileGroupName)
                .ConfigureAwait(false);
            if (data is null)
            {

                // 当数据为空时，可根据业务需求添加具体逻辑，这里暂时记录日志
                _logger.LogWarning($"未找到文件组 {fileGroup.FileGroupName} 的数据");
               // await _notFileRepository.add(fileGroup, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // 使用更规范的日志记录方式，记录异常信息
            _logger.LogError(ex, "添加文件组时发生错误");
        }
    }

    public async ValueTask AddNotFileRepositoryAsync(NotFileRepository notFileRepository, CancellationToken cancellationToken)
    {
        if (notFileRepository is null)
        {
            ArgumentNullException.ThrowIfNull(notFileRepository);
        }

        try
        {
            var data = await _notFileRepository
                .GetNotFileRepositoryAsync(notFileRepository.NotFileRepositoryName)
                .ConfigureAwait(false);
            if (data is not null)
            {
                // 当数据已存在时，不应该抛出参数为空异常，这里可根据业务需求调整逻辑，暂时记录日志
                _logger.LogWarning($"文件仓库 {notFileRepository.NotFileRepositoryName} 已存在");
                return;
            }

            // 此处应添加实际的添加文件仓库逻辑
            // await _notFileRepository.AddAsync(notFileRepository, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加文件仓库时发生错误");
        }
    }

    public async ValueTask AddNotFileAsync(NotFile notFile, CancellationToken cancellationToken)
    {
        if (notFile is null)
        {
            throw new ArgumentNullException(nameof(notFile));
        }

        try
        {
            // 此处应添加实际的添加文件逻辑
            // await _notFileRepository.AddNotFileAsync(notFile, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加文件时发生错误");
        }
    }
}
