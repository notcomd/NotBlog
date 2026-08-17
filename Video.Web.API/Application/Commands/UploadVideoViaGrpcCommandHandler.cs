using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 通过 gRPC 调用 FileDev 服务上传视频文件和封面图片的处理器。
/// </summary>
public class UploadVideoViaGrpcCommandHandler(
    IVideoRepository videoRepository,
    IVideoCacheService cacheService,
    ILogger<UploadVideoViaGrpcCommandHandler> logger,
    IOptionsSnapshot<GrpcClientOptions> grpcOptions)
    : IRequestHandler<UploadVideoViaGrpcCommand, UploadVideoViaGrpcResult>
{
    public async Task<UploadVideoViaGrpcResult> Handler(UploadVideoViaGrpcCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting gRPC video upload: {VideoName}", command.VideoName);

        try
        {
            using var channel = GrpcChannel.ForAddress(grpcOptions.Value.FileDevGrpcAddress,
                new GrpcChannelOptions
                {
                    MaxReceiveMessageSize = grpcOptions.Value.MaxMessageSizeMb * 1024 * 1024,
                    MaxSendMessageSize = grpcOptions.Value.MaxMessageSizeMb * 1024 * 1024
                });
            var client = new FileStorage.FileStorageClient(channel);

            // 1. 通过 gRPC 上传视频文件
            var videoUploadRequest = new UploadFileRequest
            {
                UserId = command.UserId.ToString(),
                FileName = command.VideoFileName,
                FileContent = Google.Protobuf.ByteString.CopyFrom(command.VideoFileContent),
                FileIdentity = FileIdentity.FilePrivate,
                FileDescription = command.BriefIntroduction
            };
            videoUploadRequest.FileTags.AddRange(command.Tags);

            var videoUploadResponse = await client.UploadFileAsync(videoUploadRequest,
                deadline: DateTime.UtcNow.AddMinutes(10), cancellationToken: cancellationToken);

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
                    MaxHeight = 2160
                };

                var coverUploadResponse = await client.UploadImageAsync(coverUploadRequest,
                    deadline: DateTime.UtcNow.AddMinutes(5), cancellationToken: cancellationToken);

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
}

/// <summary>gRPC 客户端配置选项。</summary>
public class GrpcClientOptions
{
    public const string SectionName = "GrpcClient";
    public string FileDevGrpcAddress { get; set; } = "https://localhost:5001";
    public int MaxMessageSizeMb { get; set; } = 512;
}
