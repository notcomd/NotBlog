
using DomainCommon;

using FileDev.Domain.DomainEntities;
using FileDev.Domain.DomainService;
using FileDev.Domain.IRepository;

using NotMediator;

using RabbitMQ.Client;

namespace FileDev.Web.API.Application.Command;

public class UpLoadNotFileCommandHandler : IRequestHandler<UpLoadNotFileCommand,bool>
{

    private readonly INotFileRepository fileRepository;
    private readonly FileDevRepositoryService _fileDevRepositoryService;
    private readonly INotDateTime _notDateTime;
    private readonly ILogger<UpLoadNotFileCommandHandler> _logger;

    public UpLoadNotFileCommandHandler( INotFileRepository fileRepository, FileDevRepositoryService fileDevRepositoryService, INotDateTime notDateTime, ILogger<UpLoadNotFileCommandHandler> logger)
    {
        this.fileRepository = fileRepository;
        _fileDevRepositoryService = fileDevRepositoryService;
        _notDateTime = notDateTime;
        _logger = logger;
    }

    public async Task<bool> Handler(UpLoadNotFileCommand request, CancellationToken cancellationToken)
    {
        try
        {
           ArgumentNullException.ThrowIfNull(request, nameof(request));
           
            
            
            await fileRepository.UnitOfWork.SavaChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件时发生错误");
            return false;
        }
    }
}
