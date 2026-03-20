using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;

namespace FileDev.Domain.IServices;

public interface INotFileStorageService
{
    /// <summary>
    /// 保存文件
    /// </summary>
    NotFileStorageResponse Save(NotFileStorageRequest request);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    NotFileStorageResponse Delete(string fileRelativePath);
    
    /// <summary>
    /// 获取文件内容
    /// </summary>
    (byte[] Content, NotFileStorageResponse Response) GetContent(string fileRelativePath);
    
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    bool Exists(string fileRelativePath);

    /// <summary>
    /// 上传单个分片
    /// </summary>
    NotFileStorageResponse UploadChunk(string fileKey, int chunkIndex, byte[] chunkContent, string chunkHash = null);

    /// <summary>
    /// 合并分片为完整文件
    /// </summary>
    NotFileStorageResponse MergeChunks(string fileKey, int totalChunks, string expectedFileHash = null, bool overwrite = true);

    /// <summary>
    /// 获取总分片数
    /// </summary>
    int GetTotalChunkCount(long fileSize);
}