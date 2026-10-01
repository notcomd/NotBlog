using FileDev.Domain.Dto.Response;
using FileDev.Domain.IServices;
using Microsoft.Extensions.Logging;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;

namespace FileDev.Infrastructure.Service;

// 分片上传逻辑：分片临时对象的 key 规范、分片上传/合并/清理与总分片数计算。
// 合并时把分片按序读回拼为整对象写入 FileBox（内容寻址，重复块自动去重），随后清理临时分片。
public sealed partial class FileBoxObjectStorageService
{
    /// <summary>分片临时对象的 key 前缀，避免与最终对象（{userId}/{guid}{ext}）冲突。</summary>
    private const string ChunkPrefix = "__chunk__/";

    /// <summary>分片临时对象 key：<c>__chunk__/{fileKey}#{chunkIndex}</c>。</summary>
    private static string ChunkKey(string fileKey, int chunkIndex) => $"{ChunkPrefix}{fileKey}#{chunkIndex}";

    /// <summary>列出并删除某 fileKey 的全部分片临时对象（幂等，缺失即忽略）。</summary>
    /// <param name="fileKey">分片上传任务标识（最终对象 key）。</param>
    /// <param name="namespaceId">分片所属命名空间（按租户派生）。</param>
    private async Task CleanupChunkObjectsAsync(string fileKey, string namespaceId)
    {
        if (string.IsNullOrWhiteSpace(fileKey))
            return;

        var keys = await ListChunkKeysAsync(fileKey, namespaceId).ConfigureAwait(false);
        foreach (var key in keys)
        {
            try
            {
                var hash = await ResolveHashAsync(key, namespaceId).ConfigureAwait(false);
                if (string.IsNullOrEmpty(hash))
                    continue;
                await _delete.ExecuteAsync(new DeleteObjectCommand
                {
                    ContentHash = hash,
                    NamespaceId = namespaceId
                }, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理分片临时对象失败 Key={Key}", key);
            }
        }
    }

    /// <summary>按 key 前缀列出分片临时对象 key（经索引反查，不做物理遍历）。</summary>
    private async Task<IReadOnlyList<string>> ListChunkKeysAsync(string fileKey, string namespaceId)
    {
        try
        {
            var prefix = $"{ChunkPrefix}{fileKey}";
            var page = await _index.QueryAsync(new IndexQuery
            {
                NamespaceId = namespaceId,
                KeyPrefix = prefix
            }, CancellationToken.None).ConfigureAwait(false);
            return page.Items.Where(e => e.ObjectKey is not null && e.ObjectKey.StartsWith(prefix, StringComparison.Ordinal))
                .Select(e => e.ObjectKey!).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "列出分片临时对象失败 FileKey={FileKey}", fileKey);
            return [];
        }
    }

    /// <summary>
    /// 清理某上传任务的临时分片文件。
    /// 异常不向外抛出，仅记录日志，避免清理环节污染主流程。
    /// </summary>
    /// <param name="fileKey">分片上传任务标识。</param>
    /// <param name="tenantId">租户 ID（决定分片所在命名空间）。</param>
    public async Task CleanupChunksAsync(string fileKey, string? tenantId = null)
    {
        try
        {
            await CleanupChunkObjectsAsync(fileKey, NsOf(EffectiveTenant(new StoreContext(null, tenantId)))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理分片临时对象失败 FileKey={FileKey}", fileKey);
        }
    }

    /// <summary>上传单个分片：校验可选哈希后写入暂存对象 <c>__chunk__/{fileKey}#{index}</c>（默认为分片暂存池）。</summary>
    /// <param name="fileKey">分片上传任务标识。</param>
    /// <param name="chunkIndex">分片序号（从 0 开始）。</param>
    /// <param name="chunkContent">分片内容。</param>
    /// <param name="chunkHash">期望 SHA-256，非空时前置校验。</param>
    /// <param name="tenantId">租户 ID（决定分片命名空间）。</param>
    public async Task<NotFileStorageResponse> UploadChunkAsync(string fileKey, int chunkIndex, byte[] chunkContent,
        string? chunkHash = null, string? tenantId = null)
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

            var ns = NsOf(EffectiveTenant(new StoreContext(null, tenantId)));
            var key = ChunkKey(fileKey, chunkIndex);
            var putResult = await _put.ExecuteAsync(new PutObjectCommand
            {
                NamespaceId = ns,
                ObjectKey = key,
                Content = new MemoryStream(chunkContent, writable: false),
                // 分片作为暂存对象统一落默认池，合并产物才按类别落对应物理池
                Write = new Mono.FileBox.Lite.Abstractions.Storage.WriteOptions { PoolId = DefaultPoolId }
            }, CancellationToken.None).ConfigureAwait(false);

            if (!putResult.Succeeded)
            {
                _logger.LogError("上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex} Error={Error}",
                    fileKey, chunkIndex, putResult.Error);
                return Failure($"上传分片{chunkIndex}失败");
            }

            var actualHash = putResult.ContentHash;
            if (!string.IsNullOrWhiteSpace(chunkHash) && !string.Equals(actualHash, chunkHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("分片存储哈希与期望不一致 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
                return Failure($"分片{chunkIndex}哈希校验失败");
            }

            return new NotFileStorageResponse
            {
                Success = true,
                FullPath = key,
                FileSize = chunkContent.Length,
                ActualHash = actualHash,
                ContentHash = actualHash,
                VolumeId = DefaultPoolId,
                ShardCount = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分片失败 FileKey={FileKey} ChunkIndex={ChunkIndex}", fileKey, chunkIndex);
            return Failure($"上传分片{chunkIndex}失败");
        }
    }

    /// <summary>合并分片为完整文件：按序读回拼接、可选整文件哈希校验、写整对象（产物按类别落对应池）、清理临时分片。</summary>
    /// <param name="fileKey">分片上传任务标识（亦为最终对象 key）。</param>
    /// <param name="totalChunks">分片总数。</param>
    /// <param name="expectedFileHash">期望整文件 SHA-256，非空时合并后校验。</param>
    /// <param name="overwrite">是否允许覆盖已存在的最终对象。</param>
    /// <param name="ctx">存储上下文（类别决定产物落池，租户决定命名空间）。</param>
    public async Task<NotFileStorageResponse> MergeChunksAsync(string fileKey, int totalChunks,
        string? expectedFileHash = null, bool overwrite = true, StoreContext? ctx = null)
    {
        try
        {
            var ns = NsOf(EffectiveTenant(ctx));
            using var buffer = new MemoryStream();
            for (var i = 0; i < totalChunks; i++)
            {
                var chunkKey = ChunkKey(fileKey, i);
                var chunkHash = await ResolveHashAsync(chunkKey, ns).ConfigureAwait(false);
                if (string.IsNullOrEmpty(chunkHash))
                    return Failure($"分片{i}缺失");
                var chunkResult = await _get.ExecuteAsync(new GetObjectCommand
                {
                    ContentHash = chunkHash!,
                    NamespaceId = ns,
                    Offset = 0,
                    Length = null
                }, CancellationToken.None).ConfigureAwait(false);
                using var chunkStream = chunkResult.Content;
                if (chunkStream is null)
                    return Failure($"分片{i}缺失");
                chunkStream.CopyTo(buffer);
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

            // 产物按类别路由到对应物理池
            var putResult = await _put.ExecuteAsync(new PutObjectCommand
            {
                NamespaceId = ns,
                ObjectKey = fileKey,
                Content = new MemoryStream(merged, writable: false),
                Write = new Mono.FileBox.Lite.Abstractions.Storage.WriteOptions { PoolId = PoolIdOf(ctx?.Source) }
            }, CancellationToken.None).ConfigureAwait(false);

            if (!putResult.Succeeded)
            {
                _logger.LogError("合并分片失败 FileKey={FileKey} Error={Error}", fileKey, putResult.Error);
                return Failure(putResult.Error ?? "合并分片失败");
            }

            await CleanupChunkObjectsAsync(fileKey, ns).ConfigureAwait(false);
            await InvalidateCacheAsync(fileKey).ConfigureAwait(false);

            return Success(fileKey, merged, putResult.ContentHash);
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