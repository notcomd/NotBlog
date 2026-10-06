using NotMediator;

namespace Video.Web.API.Application.Commands;

/// <summary>
/// 通过 gRPC 上传视频命令 — 使用 FileDev gRPC 服务进行文件存储。
/// 实现幂等性接口防止重复提交。
/// </summary>
public record UploadVideoViaGrpcCommand(
    Guid RequestId,
    Guid UserId,
    string VideoName,
    string BriefIntroduction,
    byte[] VideoFileContent,
    string VideoFileName,
    byte[]? CoverImageContent,
    string? CoverImageFileName,
    HashSet<string> Tags,
    VideoControl VideoControl,
    bool AsDraft = true) : IRequest<UploadVideoViaGrpcResult>, IIdempotentRequest;

/// <summary>gRPC 视频上传结果。</summary>
public record UploadVideoViaGrpcResult(
    bool Success,
    Guid VideoGuid,
    string VideoFileUri,
    string? CoverFileUri,
    string? ErrorMessage);
