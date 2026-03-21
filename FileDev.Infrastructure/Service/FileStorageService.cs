using System.Text;
using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Options;
using Notcomd.Token.JWT;

namespace FileDev.Infrastructure.Service;

public class FileStorageService(
    INotFileStorageService storageProvider,
    IOptionsSnapshot<NotFileStorageOptions> configOptions)
{
    private readonly NotFileStorageOptions _config = configOptions.Value;

    #region 基础方法（兼容原有逻辑）

    /// <summary>
    /// 保存文本文件（带哈希校验）
    /// </summary>
    public async Task<NotFileStorageResponse> SaveTextFileAsync(string fileRelativePath, string content,
        string? expectedHash = null,
        string? encoding = null, bool overwrite = true)
    {
        var encode = encoding ?? _config.DefaultEncoding;
        var bytes = Encoding.GetEncoding(encode).GetBytes(content);
        var request = new NotFileStorageRequest
        {
            FileRelativePath = fileRelativePath,
            FileContent = bytes,
            Overwrite = overwrite,
            Encoding = encode,
            ExpectedHash = expectedHash
        };
        return await storageProvider.SaveAsync(request);
    }

    /// <summary>
    /// 保存二进制文件（带哈希校验）
    /// </summary>
    public async Task<NotFileStorageResponse> SaveBinaryFileAsync(string fileRelativePath, byte[] content,
        string? expectedHash = null,
        bool overwrite = true)
    {
        var request = new NotFileStorageRequest
        {
            FileRelativePath = fileRelativePath,
            FileContent = content,
            Overwrite = overwrite,
            ExpectedHash = expectedHash
        };
        return await storageProvider.SaveAsync(request);
    }

    public async Task<NotFileStorageResponse> DeleteFileAsync(string fileRelativePath) =>
        await storageProvider.DeleteAsync(fileRelativePath); // Changed from return to await return

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public async Task<(byte[] Content, NotFileStorageResponse Response)> GetFileContentAsync(string fileRelativePath) =>
        await storageProvider.GetContentAsync(fileRelativePath);

    /// <summary>
    /// 判断文件是否存在
    /// </summary>
    /// <param name="fileRelativePath"></param>
    /// <returns></returns>
    public async Task<bool> FileExistsAsync(string fileRelativePath) =>
        await storageProvider.ExistsAsync(fileRelativePath);

    #endregion

    #region 分片上传相关方法

    /// <summary>
    /// 获取文件总分片数
    /// </summary>
    public async Task<int> GetTotalChunkCountAsync(long fileSize)
    {
        return await storageProvider.GetTotalChunkCountAsync(fileSize);
    }

    /// <summary>
    /// 上传单个分片（自动计算分片哈希）
    /// </summary>
    public async Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        bool autoVerify = true)
    {
        // 自动计算分片哈希并校验
        string chunkHash = (autoVerify ? HashHelper.ComputeHash(chunkContent, _config.HashAlgorithm) : null) ??
                           throw new InvalidOperationException();
        return await storageProvider.UploadChunkAsync(fileKey, chunkIndex, chunkContent, chunkHash);
    }

    /// <summary>
    /// 合并分片（自动校验整体文件哈希）
    /// </summary>
    public async Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks,
        byte[] originalFileContent = null,
        bool overwrite = true)
    {
        // 如果传入原文件内容，自动计算整体哈希并校验
        var expectedFileHash = originalFileContent != null
            ? HashHelper.ComputeHash(originalFileContent, _config.HashAlgorithm)
            : null;
        return await storageProvider.MergeChunksAsync(fileKey, totalChunks, expectedFileHash ?? string.Empty,
            overwrite);
    }

    /// <summary>
    /// 快速分片上传大文件（封装分片+合并逻辑）
    /// </summary>
    /// <param name="fileKey">文件最终存储路径</param>
    /// <param name="fileContent">大文件字节内容</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <returns>最终结果</returns>
    public async Task<NotFileStorageResponse> UploadBigFileByChunkAsync(string fileKey, byte[] fileContent,
        bool overwrite = true)
    {
        // 1. 获取总分片数
        int totalChunks = await GetTotalChunkCountAsync(fileContent.Length);
        if (totalChunks == 1)
        {
            // 小于分片大小，直接保存
            return await SaveBinaryFileAsync(fileKey, fileContent,
                HashHelper.ComputeHash(fileContent, _config.HashAlgorithm),
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
            var chunkResult = await UploadChunkAsync(fileKey, i, chunkContent, true);
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
        return await MergeChunksAsync(fileKey, totalChunks, fileContent, overwrite);
    }

    #endregion
}