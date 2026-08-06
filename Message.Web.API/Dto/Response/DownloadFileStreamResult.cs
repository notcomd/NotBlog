namespace Message.Web.API.Dto.Response;
/// <summary>文件流式下载结果（FileDev gRPC DownloadFile 的封装；Chunks 为响应流分片）</summary>
/// <remarks>文件名/大小/类型等元数据由 Message 侧附件记录提供，流仅承载二进制分片。</remarks>
public record DownloadFileStreamResult(
    bool Success,
    IAsyncEnumerable<byte[]> Chunks,
    string? ErrorMessage);

