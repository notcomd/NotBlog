using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace FileDev.Web.API;

public class NotFileStorageService(
    INotFileStorageService storageProvider,
    IOptions<NotFileStorageOptions> configOptions)
{
    private readonly NotFileStorageOptions _config = configOptions.Value;

    #region 基础方法（兼容原有逻辑）

    /// <summary>
    /// 保存文本文件（带哈希校验）
    /// </summary>
    public NotFileStorageResponse SaveTextFile(string fileRelativePath, string content, string? expectedHash = null,
        string? encoding = null, bool overwrite = true)
    {
        var encode = encoding ?? _config.DefaultEncoding;
        var bytes = System.Text.Encoding.GetEncoding(encode).GetBytes(content);
        var request = new NotFileStorageRequest
        {
            FileRelativePath = fileRelativePath,
            FileContent = bytes,
            Overwrite = overwrite,
            Encoding = encode,
            ExpectedHash = expectedHash
        };
        return storageProvider.Save(request);
    }

    /// <summary>
    /// 保存二进制文件（带哈希校验）
    /// </summary>
    public NotFileStorageResponse SaveBinaryFile(string fileRelativePath, byte[] content, string expectedHash = null,
        bool overwrite = true)
    {
        var request = new NotFileStorageRequest
        {
            FileRelativePath = fileRelativePath,
            FileContent = content,
            Overwrite = overwrite,
            ExpectedHash = expectedHash
        };
        return storageProvider.Save(request);
    }

    public NotFileStorageResponse DeleteFile(string fileRelativePath) => storageProvider.Delete(fileRelativePath);

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public (byte[] Content, NotFileStorageResponse Response) GetFileContent(string fileRelativePath) =>
        storageProvider.GetContent(fileRelativePath);

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public bool FileExists(string fileRelativePath) => storageProvider.Exists(fileRelativePath);

    #endregion

    #region 分片上传相关方法

    /// <summary>
    /// 获取文件总分片数
    /// </summary>
    public int GetTotalChunkCount(long fileSize)
    {
        return storageProvider.GetTotalChunkCount(fileSize);
    }

    /// <summary>
    /// 上传单个分片（自动计算分片哈希）
    /// </summary>
    public NotFileStorageResponse UploadChunk(string fileKey, int chunkIndex, byte[] chunkContent,
        bool autoVerify = true)
    {
        // 自动计算分片哈希并校验
        string chunkHash = (autoVerify ? HashHelper.ComputeHash(chunkContent, _config.HashAlgorithm) : null) ?? throw new InvalidOperationException();
        return storageProvider.UploadChunk(fileKey, chunkIndex, chunkContent, chunkHash);
    }

    /// <summary>
    /// 合并分片（自动校验整体文件哈希）
    /// </summary>
    public NotFileStorageResponse MergeChunks(string fileKey, int totalChunks, byte[] originalFileContent = null,
        bool overwrite = true)
    {
        // 如果传入原文件内容，自动计算整体哈希并校验
        string expectedFileHash = originalFileContent != null
            ? HashHelper.ComputeHash(originalFileContent, _config.HashAlgorithm)
            : null;
        return storageProvider.MergeChunks(fileKey, totalChunks, expectedFileHash, overwrite);
    }

    /// <summary>
    /// 快速分片上传大文件（封装分片+合并逻辑）
    /// </summary>
    /// <param name="fileKey">文件最终存储路径</param>
    /// <param name="fileContent">大文件字节内容</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <returns>最终结果</returns>
    public NotFileStorageResponse UploadBigFileByChunk(string fileKey, byte[] fileContent, bool overwrite = true)
    {
        // 1. 获取总分片数
        int totalChunks = GetTotalChunkCount(fileContent.Length);
        if (totalChunks == 1)
        {
            // 小于分片大小，直接保存
            return SaveBinaryFile(fileKey, fileContent, HashHelper.ComputeHash(fileContent, _config.HashAlgorithm),
                overwrite);
        }

        // 2. 拆分并上传所有分片
        long chunkSize = _config.ChunkFileSize;
        for (int i = 0; i < totalChunks; i++)
        {
            // 计算分片起始和结束位置
            long start = i * chunkSize;
            long end = Math.Min((i + 1) * chunkSize, fileContent.Length);
            int chunkLength = (int)(end - start);

            // 提取分片内容
            byte[] chunkContent = new byte[chunkLength];
            Array.Copy(fileContent, start, chunkContent, 0, chunkLength);

            // 上传分片（自动校验）
            var chunkResult = UploadChunk(fileKey, i, chunkContent, true);
            if (!chunkResult.Success)
            {
                return new NotFileStorageResponse
                {
                    Success = false,
                    ErrorMessage = $"分片{i}上传失败：{chunkResult.ErrorMessage}"
                };
            }
        }

        // 3. 合并分片
        return MergeChunks(fileKey, totalChunks, fileContent, overwrite);
    }

    #endregion
}