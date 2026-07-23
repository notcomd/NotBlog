namespace FileDev.Web.API.Application.Command;

using FileDev.Domain.IRepository;

public class CreateNotFileCommandHandler(INotFileStorageService storageProvider,
                                         IOptionsSnapshot<NotFileStorageOptions> configOptions,
                                         INotFileRepository notFileRepository,
                                         ILogger<CreateNotFileCommandHandler> logger)
    : NotMediator.IRequestHandler<CreateNotFileCommand, bool>
{
    private readonly NotFileStorageOptions _config =
        configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));

    private readonly ILogger<CreateNotFileCommandHandler> _logger = logger;
    private readonly INotFileRepository _notFileRepository =
        notFileRepository ?? throw new ArgumentNullException(nameof(notFileRepository));

    private readonly INotFileStorageService _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<bool> Handler(CreateNotFileCommand request, CancellationToken cancellationToken)
    {
        // 1. 校验
        if (request.UserGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(request));
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空", nameof(request));

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_config.AllowedExtensions.Contains(ext))
            throw new ArgumentException($"不支持的文件类型: {ext}");

        if (request.FileSize > _config.MaxFileSize)
            throw new ArgumentException(
                $"文件大小 {request.FileSize} 超过限制 {_config.MaxFileSize / 1024 / 1024}MB");

        // 2. 构建实体并插入跟踪器（实体构造时会添加 UploadNotFileEvent 领域事件）
        var notfile = new NotFile.NotFileBuilder()
            .WithUserId(request.UserGuid)
            .WithFileName(request.FileName)
            .WithFileTags(request.FileTags ?? [])
            .WithFileDescription(request.FileDescription ?? string.Empty)
            .WithFileSize(request.FileSize)
            .WithFileUri(request.FilePath)
            .WithFileMd5(request.FileMd5)
            .WithFileIdentity(request.FileIdentity)
            .Build();

        await _notFileRepository.InsertFileAsync(notfile);

        // TransactionBehavior 会在 SavaEntitiesAsync 时触发领域事件分发，
        // UploadNotFileEventHandler 自动将文件关联到根组
        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is CreateNotFileCommandHandler handler &&
               EqualityComparer<ILogger<CreateNotFileCommandHandler>?>.Default.Equals(_logger, handler._logger);
    }

    public class CreateNotFileIdentifiedCommandHandler(
        INotMediator mediator,
        IRequestManagement requestManagement,
        ILogger<IdentifiedCommandHandler<CreateNotFileCommand, bool>> logger)
        : IdentifiedCommandHandler<CreateNotFileCommand, bool>(mediator, requestManagement, logger)
    {
        protected override bool CreateResultForDuplicateRequest() => true;
    }
}
