using Grpc.Core;
using Microsoft.AspNetCore.Http;
using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 通过 gRPC 调用 FileDev 服务上传视频文件和封面图片的处理器。
/// <para>
/// 鉴权说明：FileDev 的 gRPC 拦截器（GrpcJwtAuthInterceptor）要求请求携带 Bearer JWT。
/// 本服务由登录用户的 HTTP 请求触发，故通过 <see cref="IHttpContextAccessor"/> 转发现场用户的令牌，
/// 以确保 FileDev 能解析出合法调用者身份（与 Message 服务一致），并透传关联的 content_id/content_type。
/// </para>
/// </summary>
public class UploadVideoViaGrpcCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<UploadVideoViaGrpcCommandHandler> logger,
    IOptionsSnapshot<GrpcClientOptions> grpcOptions,
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<UploadVideoViaGrpcCommand, UploadVideoViaGrpcResult>
{
    /// <summary>FileDev gRPC 的命名 HttpClient 名（Program.cs 已注册，ServiceDefaults 注入服务发现）。</summary>
    public const string FileDevGrpcHttpClientName = "filedev-web-api";

    /// <summary>FileDev gRPC 虚拟主机名（与 AppHost 注册服务名一致，经 Aspire 服务发现解析）。</summary>
    private const string FileDevGrpcVirtualHost = "https://filedev-web-api";

    public async Task<UploadVideoViaGrpcResult> Handler(UploadVideoViaGrpcCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting gRPC video upload: {VideoName}", command.VideoName);

        try
        {
            // 地址优先经 Aspire 服务发现解析服务名；独立运行时用 GrpcClient:FileDevGrpcAddress 配置兜底（如 https://localhost:9093）
            var configuredAddress = grpcOptions.Value.FileDevGrpcAddress;
            var targetAddress = string.IsNullOrWhiteSpace(configuredAddress)
                ? FileDevGrpcVirtualHost
                : configuredAddress;

            using var channel = GrpcChannel.ForAddress(targetAddress,
                new GrpcChannelOptions
                {
                    // 复用命名 HttpClient：携带 ServiceDefaults 注入的服务发现解析器 / 证书处理管道
                    HttpClient = httpClientFactory.CreateClient(FileDevGrpcHttpClientName),
                    MaxReceiveMessageSize = grpcOptions.Value.MaxMessageSizeMb * 1024 * 1024,
                    MaxSendMessageSize = grpcOptions.Value.MaxMessageSizeMb * 1024 * 1024
                });
            var client = new FileStorage.FileStorageClient(channel);

            // 视频附件使用同一 content_id 归组，上传即建立内容弱引用（CONTENT_TYPE_VIDEO）
            var contentId = Guid.NewGuid().ToString();

            // 1. 通过 gRPC 上传视频文件
            var videoUploadRequest = new UploadFileRequest
            {
                UserId = command.UserId.ToString(),
                FileName = command.VideoFileName,
                FileContent = Google.Protobuf.ByteString.CopyFrom(command.VideoFileContent),
                FileIdentity = FileIdentity.FilePrivate,
                FileDescription = command.BriefIntroduction,
                ContentId = contentId,
                ContentType = ContentType.Video
            };
            videoUploadRequest.FileTags.AddRange(command.Tags);

            var videoUploadResponse = await client.UploadFileAsync(videoUploadRequest,
                BuildCallOptions(TimeSpan.FromMinutes(10), cancellationToken));

            if (!videoUploadResponse.Success)
                return new UploadVideoViaGrpcResult(false, Guid.Empty, "", null,
                    $"Video upload failed: {videoUploadResponse.ErrorMessage}");

            logger.LogInformation("Video file uploaded via gRPC: {FileUri}", videoUploadResponse.FileUri);

            // 2. 上传封面图片（如果有）
            string? coverFileUri = null;
            if (command.CoverImageContent is { Length: > 0 } && !string.IsNullOrWhiteSpace(command.CoverImageFileName))
            {
                var coverUploadRequest = new UploadImageRequest
                {
                    UserId = command.UserId.ToString(),
                    FileName = command.CoverImageFileName,
                    ImageContent = Google.Protobuf.ByteString.CopyFrom(command.CoverImageContent),
                    FileIdentity = FileIdentity.FilePrivate,
                    ValidateFormat = true,
                    MaxWidth = 3840,
                    MaxHeight = 2160,
                    ContentId = contentId,
                    ContentType = ContentType.Video
                };

                var coverUploadResponse = await client.UploadImageAsync(coverUploadRequest,
                    BuildCallOptions(TimeSpan.FromMinutes(5), cancellationToken));

                if (!coverUploadResponse.Success)
                    return new UploadVideoViaGrpcResult(false, Guid.Empty,
                        videoUploadResponse.FileUri, null,
                        $"Cover image upload failed: {coverUploadResponse.ErrorMessage}");

                coverFileUri = coverUploadResponse.FileUri;
                logger.LogInformation("Cover image uploaded via gRPC: {FileUri}", coverFileUri);
            }

            // 3. 创建视频实体并持久化
            var coverUri = new Uri(coverFileUri ?? videoUploadResponse.FileUri, UriKind.RelativeOrAbsolute);
            var videoFileUri = new Uri(videoUploadResponse.FileUri, UriKind.RelativeOrAbsolute);

            var videoAuthorizes = new HashSet<Guid> { command.UserId };
            var video = new Videos(videoAuthorizes.ToList(), command.VideoName, coverUri,
                videoFileUri, command.BriefIntroduction, command.Tags.ToList());
            video.VideoControlChangeByVideoController(command.VideoControl);

            // 非草稿创建：立即提交审核（进入待审核状态，不直接发布）
            if (!command.AsDraft)
                video.SubmitForReview();

            await videoRepository.AddByVideoAsync(video);
            await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            // 4. 触发视频发布领域事件
            video.AddDomainEvent(new DomainEvents.VideoPublishedDomainEvent(
                video.VideoGuid, command.VideoName, coverUri));

            await videoRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            // P-04：新增视频后失效视频列表缓存，确保新视频立即可见
            await cacheService.InvalidateVideoListsAsync(cancellationToken);

            logger.LogInformation("Video entity created: {VideoGuid} {VideoName}",
                video.VideoGuid, command.VideoName);

            return new UploadVideoViaGrpcResult(true, video.VideoGuid,
                videoUploadResponse.FileUri, coverFileUri, null);
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC call failed for video upload: {VideoName}", command.VideoName);
            return new UploadVideoViaGrpcResult(false, Guid.Empty, "", null,
                $"gRPC error: {ex.Status.Detail}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload video: {VideoName}", command.VideoName);
            return new UploadVideoViaGrpcResult(false, Guid.Empty, "", null, ex.Message);
        }
    }

    /// <summary>
    /// 构建 gRPC 调用选项：转发当前登录用户的 Bearer token 作为鉴权头（FileDev 拦截器要求）。
    /// ⚠️ 必须用 IHttpContextAccessor（Singleton + AsyncLocal）而非 ICurrentUserService：
    /// NotMediator 的 mediator 是 Singleton，SendAsync 内部 CreateScope() 解析 handler——
    /// 本 handler 拿到的是新 scope 的 ICurrentUserService 实例（令牌恒空），而令牌设在 HTTP 请求 scope 上。
    /// </summary>
    private CallOptions BuildCallOptions(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var headers = new Metadata();
        var token = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(token) &&
            token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            headers.Add("Authorization", token.Trim());
        }
        else
        {
            logger.LogWarning("[VideoGrpc] ⚠️ gRPC 调用缺少 Bearer token（HttpContext 中无 Authorization 头）");
        }

        return new CallOptions(
            headers: headers,
            cancellationToken: cancellationToken,
            deadline: DateTime.UtcNow + timeout);
    }
}

/// <summary>gRPC 客户端配置选项。</summary>
public class GrpcClientOptions
{
    public const string SectionName = "GrpcClient";
    public string FileDevGrpcAddress { get; set; } = "https://localhost:9093";
    public int MaxMessageSizeMb { get; set; } = 512;
}
