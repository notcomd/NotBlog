// 修正拼写错误
using DomainCommon;

using FileDev.Domain.DomainEntities;
using FileDev.Domain.DomainService;
using FileDev.Domain.IRepository;

using NotMediator;

namespace FileDev.Web.API.Application.Command;

public class CreateFileGroupCommandHandler : IRequestHandler<CreateFileGroupCommand, bool>
{

    private readonly INotFileRepository _fileRepository;
    private readonly IFileGroupRepository _fileGroupRepository;

    private readonly FileDevRepositoryService _fileDevRepositoryService;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<CreateFileGroupCommandHandler> _logger;

    public CreateFileGroupCommandHandler(INotFileRepository fileRepository, IFileGroupRepository fileGroupRepository, FileDevRepositoryService fileDevRepositoryService, INotDateTime notDateTime, ILogger<CreateFileGroupCommandHandler> logger)
    {
        _fileRepository = fileRepository;
        _fileGroupRepository = fileGroupRepository;
        _fileDevRepositoryService = fileDevRepositoryService;
        _notDateTime = notDateTime;
        _logger = logger;
       // _fileGroupRepository = fileRepository;
    }

    // 方法名拼写错误，应为 Handle
    public async Task<bool> Handler(CreateFileGroupCommand request, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));
            // 验证请求参数的关键属性
            if (string.IsNullOrWhiteSpace(request.FileGroupName))
            {
                _logger.LogError("文件组名称不能为空");
                return false;
            }
            // 检查文件组是否已存在
            if (await _fileGroupRepository.QueryFileGroupByNameAsync(request.FileGroupName) is not null)
            {
                _logger.LogError($"文件组 {request.FileGroupName} 已存在");
                return false;
            }
            // 创建文件组实体
            var fileGroup = new FileGroup(            
                request.FileGroupBelongToUserGuid,
                request.FileGroupBelongToFileRepositoryGuid,
                request.FileGroupName,
                request.FileGroupSafety,
                request.CreateFileGroupDate,
                request.FileGroupTags
                );
          
            await _fileGroupRepository.CreateFileGroupAsync(fileGroup);
            
        
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("操作被取消");
            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建文件组时发生错误");
            return false;
        }
    }
}
