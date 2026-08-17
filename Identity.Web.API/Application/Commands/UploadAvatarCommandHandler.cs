using Google.Protobuf;
using Identity.Web.API.Grpc;

namespace Identity.Web.API.Application.Commands;

public class UploadAvatarCommandHandler
    : IRequestHandler<UploadAvatarCommand, UploadAvatarResult>
{
    private readonly FileStorage.FileStorageClient _grpcClient;
    private readonly ILogger<UploadAvatarCommandHandler> _logger;

    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private const int MaxWidth = 2000;
    private const int MaxHeight = 2000;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    public UploadAvatarCommandHandler(
        FileStorage.FileStorageClient grpcClient,
        ILogger<UploadAvatarCommandHandler> logger)
    {
        _grpcClient = grpcClient ?? throw new ArgumentNullException(nameof(grpcClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UploadAvatarResult> Handler(
        UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[UploadAvatar] 开始处理头像上传: UserId={UserId}, FileName={FileName}, Size={Size}",
            command.UserId, command.FileName, command.ImageContent.Length);

        // ── 1. 文件大小校验 ──
        if (command.ImageContent.Length == 0)
            throw new InvalidOperationException("图片内容不能为空");

        if (command.ImageContent.Length > MaxFileSize)
            throw new InvalidOperationException(
                $"图片大小超过限制 {MaxFileSize / 1024 / 1024}MB");

        // ── 2. 文件扩展名校验 ──
        var ext = Path.GetExtension(command.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"不支持的图片格式: {ext}，仅支持 {string.Join(", ", AllowedExtensions)}");

        // ── 3. Content-Type 校验 ──
        if (!string.IsNullOrEmpty(command.ContentType) &&
            !AllowedContentTypes.Contains(command.ContentType))
            throw new InvalidOperationException(
                $"不支持的 Content-Type: {command.ContentType}");

        // ── 4. 调用 gRPC 上传图片 ──
        var request = new UploadImageRequest
        {
            UserId = command.UserId.ToString(),
            FileName = command.FileName,
            ImageContent = ByteString.CopyFrom(command.ImageContent),
            FileTags = { "avatar" },
            FileDescription = "用户头像",
            FileIdentity = FileIdentity.FilePrivate,
            ValidateFormat = true,
            MaxWidth = MaxWidth,
            MaxHeight = MaxHeight
        };

        _logger.LogInformation(
            "[UploadAvatar] 调用 gRPC UploadImage: UserId={UserId}, FileName={FileName}",
            command.UserId, command.FileName);

        UploadImageResponse response;
        try
        {
            // S-08：FileDev gRPC 拦截器要求 Bearer JWT —— 转发当前请求的原始 token
            var headers = new global::Grpc.Core.Metadata();
            if (!string.IsNullOrWhiteSpace(command.AccessToken))
            {
                headers.Add("Authorization", $"Bearer {command.AccessToken}");
            }

            response = await _grpcClient.UploadImageAsync(
                request, headers, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[UploadAvatar] gRPC 调用失败: UserId={UserId}, FileName={FileName}",
                command.UserId, command.FileName);
            throw new InvalidOperationException($"文件上传服务调用失败: {ex.Message}", ex);
        }

        // ── 5. 处理 gRPC 响应 ──
        if (!response.Success)
        {
            _logger.LogWarning(
                "[UploadAvatar] gRPC 返回失败: UserId={UserId}, Error={Error}",
                command.UserId, response.ErrorMessage);
            throw new InvalidOperationException(
                $"图片上传失败: {response.ErrorMessage}");
        }

        _logger.LogInformation(
            "[UploadAvatar] 头像上传成功: UserId={UserId}, FileId={FileId}, " +
            "Width={Width}, Height={Height}, Format={Format}",
            command.UserId, response.FileId, response.Width, response.Height, response.Format);

        return new UploadAvatarResult(
            response.FileId,
            response.FileUri,
            response.FileMd5,
            response.FileSize,
            response.Width,
            response.Height,
            response.Format);
    }
}
