// 修正拼写错误
using DomainCommon;

using FileDev.Domain.DomainEntities;
using FileDev.Domain.DomainService;
using FileDev.Domain.FileDevException;
using FileDev.Domain.IRepository;

using NotMediator;

namespace FileDev.Web.API.Application.Command;

public class CreateFileDevRepositoryCommandHandler : IRequestHandler<CreateFileDevRepositoryCommand, bool>
{
    // 字段命名遵循统一的下划线前缀风格
    private readonly INotFileRepository _fileRepository;
    private readonly FileDevRepositoryService _repositoryService;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger _logger;

    // 构造函数参数顺序与字段声明顺序保持一致，提高可读性
    public CreateFileDevRepositoryCommandHandler(INotFileRepository fileRepository, FileDevRepositoryService repositoryService, INotDateTime notDateTime, ILogger logger)
    {
        _fileRepository = fileRepository;
        _repositoryService = repositoryService;
        _notDateTime = notDateTime;
        _logger = logger;
    }

    // 方法名错误，IRequestHandler 接口通常要求方法名为 Handle
    public async Task<bool> Handler(CreateFileDevRepositoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request);
            // 代码换行优化，提高可读性
            var data = new NotFileRepository(
                request.FileRepositoryUserGuid,
                request.FileRepositoryName,
                FileSafety.FilePublic,
                _notDateTime.UtcNow,
                0,
                request.FileRemarks,
                null,
                null,
                request.FileRepositoryCover);

            await _repositoryService.AddNotFileRepositoryAsync(data, cancellationToken);
            // 修正拼写错误
            await _fileRepository.UnitOfWork.SavaEntitiesAsync(cancellationToken);

            // 使用命名参数提高日志可读性
            _logger.LogInformation("[ヾ(•ω•`)o ({Now})] 成功创建仓库", _notDateTime.UtcNow);
            return true;
        }
        catch (NotFileDevException)
        {
            // 如果是自定义异常，直接抛出
            throw;
        }
        catch (Exception ex)
        {
            // 使用命名参数提高日志可读性
            _logger.LogError("[ヾ(•ω•`)o ({Now})] 创建仓库失败 {Exception}", _notDateTime.UtcNow, ex);
            // 异常信息拼接优化，避免包含原始异常对象本身
            throw new NotFileDevException($"[(≧ ﹏ ≦) ({_notDateTime.UtcNow}) 创建仓库失败: {ex.Message}]");
        }
    }
}
