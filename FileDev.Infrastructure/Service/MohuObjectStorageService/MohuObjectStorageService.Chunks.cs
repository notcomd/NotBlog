using Microsoft.Extensions.Logging;
using MohuTianchi.Lite;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;

namespace FileDev.Infrastructure.Service;

// 分片上传逻辑：分片临时对象的 key 规范、分片上传/合并/清理与总分片数计算。
// 合并时把分片按序读回拼为整对象写入 Lite（内核再次分片 + 块级去重），随后清理临时分片。
public sealed partial class MohuObjectStorageService
{
    /// <summary>分片临时对象的 key 前缀，避免与最终对象（{userId}/{guid}{ext}）冲突。</summary>
    private const string ChunkPrefix = "__chunk__/";

    /// <summary>分片临时对象 key：<c>__chunk__/{fileKey}#{chunkIndex}</c>。</summary>
    private static string ChunkKey(string fileKey, int chunkIndex) => $"{ChunkPrefix}{fileKey}#{chunkIndex}";

    /// <summary>列出并删除某 fileKey 的全部分片临时对象（幂等，缺失即忽略）。</summary>
    /// <param name="fileKey">分片上传任务标识（最终对象 key）。</param>
    private async Task CleanupChunkObjectsAsync(string fileKey)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return;

        var keys = await _storage.ListAsync($"{ChunkPrefix}{fileKey}", ct: CancellationToken.None).ConfigureAwait(false);
        foreach (var key in keys)
        {
            try
            {
                await _storage.DeleteAsync(key, ct: CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理分片临时对象失败 Key={Key}", key);
            }
        }
    }

    /// <summary>
    /// 清理某上传任务的临时分片文件。
    /// 异常不向外抛出，仅记录日志，避免清理环节污染主流程。
    /// </summary>
    /// <param name="fileKey">分片上传任务标识。</param>
    public async Task CleanupChunksAsync(string fileKey)
    {
        try
        {
            await CleanupChunkObjectsAsync(fileKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理分片临时对象失败 FileKey={FileKey}", fileKey);
        }
    }

    /// <summary>上传单个分片：校验可选哈希后写入临时对象 <c>__chunk__/{fileKey}#{index}</c>。</summary>
    /// <param name="fileKey">分片上传任务标识。</param>
    /// <param name="chunkIndex">分片序号（从 0 开始）。</param>
    /// <param name="chunkContent">分片内容。</param>
    /// <param name="chunkHash">期望 SHA-256，非空时前置校验。</param>
    public async Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string? chunkHash = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(chunkHash))
            {
                var actual = HashHelper.ComputeHash(chunkContent, AlgorithmType.SHA256);
                if (!string.Equals(actual, chunkHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("分片哈希校验失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
                    return Failure($"分片{chunkIndex}哈希校验失败");
                }
            }

            var key = ChunkKey(fileKey, chunkIndex);
            await _storage.WriteAsync(key, chunkContent, new WriteOptions { Overwrite = true }).ConfigureAwait(false);

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = key,
                FileSize = chunkContent.Length,
                ActualHash = HashHelper.ComputeHash(chunkContent, AlgorithmType.SHA256)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
            return Failure($"上传分片{chunkIndex}失败");
        }
    }

    /// <summary>合并分片为完整文件：按序读回拼接、可选整文件哈希校验、写整对象、清理临时分片并回填清单元数据。</summary>
    /// <param name="fileKey">分片上传任务标识（亦为最终对象 key）。</param>
    /// <param name="totalChunks">分片总数。</param>
    /// <param name="expectedFileHash">期望整文件 SHA-256，非空时合并后校验。</param>
    /// <param name="overwrite">是否允许覆盖已存在的最终对象。</param>
    public async Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks,
        string? expectedFileHash = null, bool overwrite = true)
    {
        try
        {
            using var buffer = new MemoryStream();
            for (var i = 0; i < totalChunks; i++)
            {
                var chunkKey = ChunkKey(fileKey, i);
                var chunkContent = await _storage.ReadAsync(chunkKey).ConfigureAwait(false);
                if (chunkContent is null)
                    return Failure($"分片{i}缺失");
                await buffer.WriteAsync(chunkContent).ConfigureAwait(false);
            }

            var merged = buffer.ToArray();

            if (!string.IsNullOrWhiteSpace(expectedFileHash))
            {
                var actualHash = HashHelper.ComputeHash(merged, AlgorithmType.SHA256);
                if (!string.Equals(actualHash, expectedFileHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("文件合并后哈希校验失败 FileKey={FileKey}", fileKey);
                    return Failure($"文件合并后哈希校验失败，预期：{expectedFileHash}，实际：{actualHash}");
                }
            }

            await _storage.WriteAsync(fileKey, merged, new WriteOptions { Overwrite = overwrite }).ConfigureAwait(false);

            await CleanupChunkObjectsAsync(fileKey).ConfigureAwait(false);

            await InvalidateCacheAsync(fileKey).ConfigureAwait(false);

            var response = new NotFileStorageResponse
            {
                Success = true,
                FullPath = fileKey,
                FileSize = merged.Length,
                ActualHash = HashHelper.ComputeHash(merged, AlgorithmType.SHA256)
            };
            // 对齐 Lite 清单元数据（卷 ID / 分片数 / 更新时间）
            response.ContentHash = response.ActualHash;
            await ApplyManifestMetaAsync(response, fileKey).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "合并分片失败 FileKey={FileKey}", fileKey);
            return Failure("合并分片失败");
        }
    }

    /// <summary>按文件大小与分片大小计算总分片数（向上取整；非正大小视为 1 片）。</summary>
    /// <param name="fileSize">文件大小（字节）。</param>
    /// <returns>总分片数。</returns>
    public Task<int> GetTotalChunkCountAsync(long fileSize)
    {
        if (fileSize <= 0) return Task.FromResult(1);
        return Task.FromResult((int)Math.Ceiling((double)fileSize / _config.ChunkFileSize));
    }
}