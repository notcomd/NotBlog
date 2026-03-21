using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;

namespace FileDev.Domain.IServices;

public interface INotFileStorageService
{
    /// <summary>
    /// 保存文件
    /// </summary>
    Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request);

    /// <summary>
    /// 删除文件
    /// </summary>
    Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath);

    /// <summary>
    /// 获取文件内容
    /// </summary>
    Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath);

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    Task<bool> ExistsAsync(string fileRelativePath);

    /// <summary>
    /// 上传单个分片
    /// </summary>
    Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string chunkHash = null);

    /// <summary>
    /// 合并分片为完整文件
    /// </summary>
    Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks, string expectedFileHash = null,
        bool overwrite = true);

    /// <summary>
    /// 获取总分片数
    /// </summary>
    Task<int> GetTotalChunkCountAsync(long fileSize);
}