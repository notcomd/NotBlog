using Message.Web.API.Dto.Response;

namespace Message.Web.API.Grpc;

/// <summary>
/// 文件存储 gRPC 客户端抽象。
/// 所有文件上传/分片上传能力必须经由 FileDev.Web.API 的 gRPC 服务提供，
/// 本接口封装了完整的请求/响应处理、断点续传、上传进度反馈与异常处理流程。
/// </summary>
public interface IFileStorageGrpcClient
{
    /// <summary>
    /// 小文件上传（单次请求，适用于 &lt;=10MB 文件）。
    /// </summary>
    /// <param name="userId">所属用户ID</param>
    /// <param name="fileName">文件名（含扩展名）</param>
    /// <param name="content">文件内容</param>
    /// <param name="description">文件描述</param>
    /// <param name="expectedMd5">预期 MD5（可选，用于一致性校验）</param>
    /// <param name="ct">取消令牌</param>
    Task<UploadFileResult> UploadFileAsync(
        Guid userId, string fileName, byte[] content,
        string? description = null, string? expectedMd5 = null,
        CancellationToken ct = default);

    /// <summary>
    /// 图片上传（携带格式校验与尺寸信息解析）。
    /// </summary>
    Task<UploadImageResult> UploadImageAsync(
        Guid userId, string fileName, byte[] content,
        string? description = null, bool validateFormat = true,
        CancellationToken ct = default);

    /// <summary>
    /// 初始化大文件分片上传，返回 fileKey 与分片参数。
    /// 服务端会持久化上传记录，便于后续断点续传。
    /// </summary>
    Task<ChunkUploadInitResult> InitChunkUploadAsync(
        Guid userId, string fileName, long totalSize,
        string? fileMd5 = null, string? description = null, bool isPublic = false,
        CancellationToken ct = default);

    /// <summary>
    /// 上传单个分片。
    /// </summary>
    Task<ChunkUploadResult> UploadChunkAsync(
        string fileKey, int chunkIndex, byte[] chunkData, string? chunkMd5 = null,
        CancellationToken ct = default);

    /// <summary>
    /// 查询分片上传状态（已上传分片索引列表），用于断点续传。
    /// </summary>
    Task<ChunkStatusResult> GetChunkStatusAsync(string fileKey, CancellationToken ct = default);

    /// <summary>
    /// 合并分片，生成最终文件并返回文件元数据。
    /// </summary>
    Task<MergeChunksResult> MergeChunksAsync(
        string fileKey, Guid userId, string? fileName = null, string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// 取消分片上传，清理服务端临时数据。
    /// </summary>
    Task<CancelChunkUploadResult> CancelChunkUploadAsync(string fileKey, CancellationToken ct = default);


    /// <summary>
    /// 查询文件信息（FileDev gRPC GetFileInfo）。
    /// 用于发送附件消息前校验文件归属：返回的 UserId 必须与当前用户一致。
    /// </summary>
    Task<FileInfoResult> GetFileInfoAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    /// 删除文件（FileDev gRPC DeleteFile），同步清理 FileDev 侧物理文件。
    /// </summary>
    Task<DeleteFileResult> DeleteFileAsync(Guid fileId, Guid userId, CancellationToken ct = default);


    /// <summary>
    /// 流式下载文件（FileDev gRPC DownloadFile，服务端流式响应）。
    /// 用于附件下载代理：权限校验与下载计数在 Message 侧完成，文件内容经此流式转发。
    /// </summary>
    Task<DownloadFileStreamResult> DownloadFileAsync(
        Guid fileId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// 流式下载图片（FileDev gRPC DownloadImage，支持服务端缩放）。
    /// 用于附件预览代理；非图片文件应提前在 Message 侧拒绝。
    /// </summary>
    Task<DownloadImageStreamResult> DownloadImageAsync(
        Guid fileId, Guid userId, int? resizeWidth = null, int? resizeHeight = null,
        CancellationToken ct = default);

    /// <summary>
    /// 断点续传：先查询服务端已上传分片，仅上传缺失分片；
    /// 每个分片上传完成后通过 <paramref name="progress"/> 回调上报进度。
    /// </summary>
    /// <param name="fileKey">分片上传记录键</param>
    /// <param name="totalChunks">总分片数</param>
    /// <param name="chunkSize">分片大小（字节）</param>
    /// <param name="chunks">待上传分片集合（索引 → 分片数据）</param>
    /// <param name="progress">上传进度回调（线程安全）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>续传完成后的分片状态</returns>
    Task<ChunkStatusResult> ResumeChunkUploadAsync(
        string fileKey, int totalChunks, int chunkSize,
        IReadOnlyDictionary<int, byte[]> chunks,
        IProgress<ChunkUploadProgress>? progress = null,
        CancellationToken ct = default);
}
