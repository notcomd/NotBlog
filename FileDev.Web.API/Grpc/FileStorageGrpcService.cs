namespace FileDev.Web.API.Grpc;

using FileDev.Domain.Dto.Request;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 文件服务外部调用接口，供其他微服务通过 HTTP/gRPC 调用。
/// 认证由 API 网关层统一处理。
/// </summary>
[ApiController]
[Route("api/external/file")]
public class FileStorageGrpcService : ControllerBase
{
    private readonly INotFileService _notFileService;
    private readonly INotFileStorageService _storageService;
    private readonly IOptionsSnapshot<NotFileStorageOptions> _options;
    private readonly ILogger<FileStorageGrpcService> _logger;

    public FileStorageGrpcService(
        INotFileService notFileService,
        INotFileStorageService storageService,
        IOptionsSnapshot<NotFileStorageOptions> options,
        ILogger<FileStorageGrpcService> logger)
    {
        _notFileService = notFileService;
        _storageService = storageService;
        _options = options;
        _logger = logger;
    }

    /// <summary>保存文件（供其他微服务调用）</summary>
    [HttpPost("save")]
    public async Task<IActionResult> SaveFile(
        [FromQuery] Guid userId,
        IFormFile file,
        [FromQuery] string? tags = null,
        [FromQuery] string? description = null,
        [FromQuery] string fileIdentity = "FilePrivate")
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "文件不能为空" });

            if (file.Length > _options.Value.MaxFileSize)
                return BadRequest(new { error = $"文件大小超过限制 {_options.Value.MaxFileSize / 1024 / 1024}MB" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.Value.AllowedExtensions.Contains(ext))
                return BadRequest(new { error = $"不支持的文件类型: {ext}" });

            var fileType = ResolveFileType(ext);
            var identity = Enum.Parse<FileIdentity>(fileIdentity);
            var fileGuid = Guid.CreateVersion7();
            var relativePath = $"{userId:N}/{fileGuid}{ext}";

            await using var stream = file.OpenReadStream();
            var content = new byte[file.Length];
            await stream.ReadExactlyAsync(content);

            var storageRequest = new NotFileStorageRequest
            {
                FileRelativePath = relativePath,
                FileContent = content,
                Overwrite = false,
                ExpectedHash = null
            };

            var storageResult = await _storageService.SaveAsync(storageRequest);
            if (!storageResult.Success)
                return StatusCode(500, new { error = storageResult.ErrorMessage });

            var fileUri = new Uri($"/files/{relativePath}", UriKind.Relative);
            var tagSet = tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            await _notFileService.CreateFileAsync(
                userId, file.FileName, tagSet, description ?? string.Empty,
                fileType, file.Length, fileUri,
                storageResult.ActualHash ?? string.Empty, identity);

            return Ok(new
            {
                success = true,
                fileUri = fileUri.ToString(),
                fileMd5 = storageResult.ActualHash,
                fileSize = file.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "外部文件保存失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>检查文件是否存在（供去重检测）</summary>
    [HttpGet("exists")]
    public async Task<IActionResult> FileExists([FromQuery] Guid fileId)
    {
        var file = await _notFileService.GetFileByIdAsync(fileId);
        return Ok(new { exists = file != null, fileId });
    }

    private static FileType ResolveFileType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileType.FileImage,
        ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" or ".flv" or ".webm" => FileType.FileVideo,
        ".mp3" or ".wav" or ".ogg" or ".flac" or ".aac" or ".wma" or ".m4a" => FileType.FileAudio,
        ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" => FileType.CompressFiles,
        _ => FileType.FileFile
    };
}
