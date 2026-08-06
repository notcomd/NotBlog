namespace Message.Web.API.Dto.Response;
/// <summary>
/// 上传进度信息。用于向 SignalR 客户端实时反馈断点续传进度。
/// </summary>
public record ChunkUploadProgress(
    string FileKey,
    int UploadedChunks,
    int TotalChunks,
    double Percent,
    int CurrentChunkIndex);

