namespace FileDev.Web.API.Application.Command;

public class CreateNotFileCommandHandler(INotFileStorageService storageProvider,
                                         IOptionsSnapshot<NotFileStorageOptions> configOptions,
                                         INotFileService notFileService,
                                         INotFileGroupService notFileGroupService,
                                         ILogger<CreateNotFileCommandHandler> logger)
    : NotMediator.IRequestHandler<CreateNotFileCommand, bool>
{
    private readonly NotFileStorageOptions _config =
        configOptions.Value ?? throw new ArgumentNullException(nameof(configOptions));

    private readonly ILogger<CreateNotFileCommandHandler> _logger = logger;
    private readonly INotFileService _notFileService =
        notFileService ?? throw new ArgumentNullException(nameof(notFileService));

    private readonly INotFileStorageService _storageProvider =
        storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));

    public async Task<bool> Handler(CreateNotFileCommand request, CancellationToken cancellationToken)
    {
        // 1. 校验
        if (request.UserGuid == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(request.UserGuid));
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ArgumentException("文件名不能为空",
                                        nameof(request.FileName));

        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!_config.AllowedExtensions.Contains(ext))
            throw new ArgumentException($"不支持的文件类型: {ext}");

        if (request.FileSize > _config.MaxFileSize)
            throw new ArgumentException(
                $"文件大小 {request.FileSize} 超过限制 {_config.MaxFileSize / 1024 / 1024}MB");

        // 2. 推断 FileType
        var fileType = ResolveFileType(ext);

        // 3. 委托给领域服务
        await _notFileService.CreateFileAsync(
            request.UserGuid,
            request.FileName,
            null,
            string.Empty,
            fileType,
            request.FileSize,
            request.FileUri,
            request.FileMd5,
            FileIdentity.FilePrivate);

        return true;
    }

    private static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };

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
