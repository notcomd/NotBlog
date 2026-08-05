namespace Message.Web.API.Dto.Response;

/// <summary>通用文件上传结果（调用 FileDev gRPC 服务后返回）</summary>
public record UploadFileResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    string? ErrorMessage);

/// <summary>图片上传结果</summary>
public record UploadImageResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    int Width,
    int Height,
    string Format,
    string? ErrorMessage);

/// <summary>分片上传初始化结果</summary>
public record ChunkUploadInitResult(
    bool Success,
    string FileKey,
    int TotalChunks,
    int ChunkSize,
    IReadOnlyList<int> UploadedChunks,
    string? ErrorMessage);

/// <summary>单个分片上传结果</summary>
public record ChunkUploadResult(
    bool Success,
    int ChunkIndex,
    string ChunkMd5,
    string? ErrorMessage);

/// <summary>分片上传状态查询结果（用于断点续传：跳过已上传分片）</summary>
public record ChunkStatusResult(
    bool Success,
    string FileKey,
    int TotalChunks,
    IReadOnlyList<int> UploadedChunks,
    string Status,
    string? ErrorMessage)
{
    /// <summary>是否所有分片均已上传完成</summary>
    public bool IsComplete => Success && TotalChunks > 0 && UploadedChunks.Count >= TotalChunks;

    /// <summary>已完成进度百分比（0-100）</summary>
    public double Percent => TotalChunks > 0 ? (double)UploadedChunks.Count / TotalChunks * 100 : 0;
}

/// <summary>分片合并结果</summary>
public record MergeChunksResult(
    bool Success,
    Guid? FileId,
    Uri? FileUri,
    string FileMd5,
    long FileSize,
    string? ErrorMessage);

/// <summary>取消分片上传结果</summary>
public record CancelChunkUploadResult(bool Success, string? ErrorMessage);

/// <summary>
/// 上传进度信息。用于向 SignalR 客户端实时反馈断点续传进度。
/// </summary>
public record ChunkUploadProgress(
    string FileKey,
    int UploadedChunks,
    int TotalChunks,
    double Percent,
    int CurrentChunkIndex);

/// <summary>
/// 文件上传/合并后的统一引用（消息与 Tweet 附件共用）。
/// <para>
/// 由 FileDev gRPC 服务返回的元数据组装；客户端持 FileId 即可在发送消息/推文时引用附件，
/// 服务端凭 FileId 回查 FileDev 校验归属并填充消息元数据。
/// </para>
/// </summary>
public record FileRef(
    Guid FileId,
    Uri FileUri,
    string FileName,
    long FileSize,
    string FileMd5,
    string MimeType,
    int? Width = null,
    int? Height = null);

/// <summary>文件信息查询结果（FileDev gRPC GetFileInfo 的封装）</summary>
public record FileInfoResult(
    bool Success,
    Guid? FileId,
    Guid? UserId,
    string FileName,
    long FileSize,
    Uri? FileUri,
    string FileMd5,
    string FileType,
    string? ErrorMessage);

/// <summary>删除文件结果（FileDev gRPC DeleteFile 的封装）</summary>
public record DeleteFileResult(bool Success, string? ErrorMessage);

/// <summary>文件流式下载结果（FileDev gRPC DownloadFile 的封装；Chunks 为响应流分片）</summary>
/// <remarks>文件名/大小/类型等元数据由 Message 侧附件记录提供，流仅承载二进制分片。</remarks>
public record DownloadFileStreamResult(
    bool Success,
    IAsyncEnumerable<byte[]> Chunks,
    string? ErrorMessage);

/// <summary>图片流式下载结果（FileDev gRPC DownloadImage 的封装）</summary>
/// <remarks>文件名/大小/类型等元数据由 Message 侧附件记录提供，流仅承载二进制分片。</remarks>
public record DownloadImageStreamResult(
    bool Success,
    IAsyncEnumerable<byte[]> Chunks,
    string? ErrorMessage);
