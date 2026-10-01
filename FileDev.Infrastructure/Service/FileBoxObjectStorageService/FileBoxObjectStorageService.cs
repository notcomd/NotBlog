using FileDev.Domain.Dto.Request;
using FileDev.Domain.Dto.Response;
using FileDev.Domain.IServices;
using FileDev.Domain.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.FileBox.Lite.Abstractions.Index;
using Mono.FileBox.Lite.Abstractions.Subsystems;
using Mono.FileBox.Lite.Abstractions.UseCases;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 基于 Mono.FileBox.Lite 内容寻址对象存储（Put/Get/Delete 用 <see cref="IPutObjectUseCase"/> 等用例 + <see cref="IIndexReader"/>）的
/// <see cref="INotFileStorageService"/> 适配器，作为 FileDev 的底层存储实现。
/// <para>
/// FileBox 是内容寻址：写入经 <see cref="IPutObjectUseCase"/> 返回 <c>ContentHash</c>，读取/删除经 ContentHash 进行。
/// 而 FileDev 契约以 <c>fileRelativePath</c> 寻址，故本类用 <see cref="IIndexReader"/> 按 <c>ObjectKey</c> 精确反查哈希做桥接
/// （path→hash 映射由持久化 JSON 索引承载，见 ModuleInitializer 的 UseJsonFileEntryStore）。
/// </para>
/// <para>
/// 本类按功能拆分为多个 partial 文件共存于同名目录：
/// 主文件（本文件）承载实例状态与文件 CRUD + 内容缓存；
/// <c>FileBoxObjectStorageService.Chunks.cs</c> 承载分片上传；<c>FileBoxObjectStorageService.Manifest.cs</c> 承载清单与分层；
/// <c>FileBoxObjectStorageService.Volumes.cs</c> 承载共享/租户数据卷与目录统计。
/// </para>
/// </summary>
public sealed partial class FileBoxObjectStorageService : INotFileStorageService
{
    /// <summary>统一命名空间前缀，FileBox 对象全部写入该命名空间。</summary>
    private const string NamespaceId = "notblog";

    /// <summary>文件内容缓存在 Redis 的 key 前缀（Base64 存储，见 <see cref="IRedisCacheService"/>）。</summary>
    private const string ContentCachePrefix = "file:content:";

    private readonly IPutObjectUseCase _put;
    private readonly IGetObjectUseCase _get;
    private readonly IDeleteObjectUseCase _delete;
    private readonly IIndexReader _index;
    private readonly IRedisCacheService _redis;
    private readonly NotFileStorageOptions _config;
    private readonly ILogger<FileBoxObjectStorageService> _logger;

    /// <summary>构造适配器。</summary>
    public FileBoxObjectStorageService(
        IPutObjectUseCase put,
        IGetObjectUseCase get,
        IDeleteObjectUseCase delete,
        IIndexReader index,
        IRedisCacheService redis,
        IOptionsSnapshot<NotFileStorageOptions> configOptions,
        ILogger<FileBoxObjectStorageService> logger)
    {
        _put = put ?? throw new ArgumentNullException(nameof(put));
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _delete = delete ?? throw new ArgumentNullException(nameof(delete));
        _index = index ?? throw new ArgumentNullException(nameof(index));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>构造失败响应。</summary>
    private static NotFileStorageResponse Failure(string message) => new() { Success = false, ErrorMessage = message };

    /// <summary>构造成功响应（占位分层元数据对齐领域的 Hot/Cold，FileBox 侧由索引条目承载）。</summary>
    private static NotFileStorageResponse Success(string path, byte[] content, string contentHash)
    {
        var response = new NotFileStorageResponse
        {
            Success = true,
            FullPath = path,
            FileSize = content.Length,
            ActualHash = contentHash,
            ContentHash = contentHash,
            Tier = Domain.Enum.StorageTier.Hot,
            VolumeId = "default",
            ShardCount = 1
        };
        return response;
    }

    /// <summary>文件内容在 Redis 的缓存 key。</summary>
    private static string CacheKey(string relativePath) => $"{ContentCachePrefix}{relativePath}";

    /// <summary>惰性回填内容缓存：仅在文件不超过大小阈值时写入 Redis，并带 TTL。缓存失败不影响主流程。</summary>
    private async Task BackfillCacheAsync(string cacheKey, byte[] content)
    {
        if (content.Length <= 0 || content.Length > _config.DownloadCacheMaxBytes)
            return;
        try
        {
            await _redis.StringSetAsync(cacheKey, Convert.ToBase64String(content),
                TimeSpan.FromSeconds(_config.DownloadCacheTtlSeconds)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "回填文件内容缓存失败 Key={Key}", cacheKey);
        }
    }

    /// <summary>失效某文件的内容缓存（删除/覆盖写之后调用，防止误读脏数据）。</summary>
    private async Task InvalidateCacheAsync(string relativePath)
    {
        try
        {
            await _redis.KeyDeleteAsync(CacheKey(relativePath)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "失效文件内容缓存失败 Path={Path}", relativePath);
        }
    }

    /// <summary>按 ObjectKey 精确反查 ContentHash（索引条目）。对象不存在返回 null。</summary>
    private async Task<string?> ResolveHashAsync(string relativePath, CancellationToken ct = default)
    {
        try
        {
            var page = await _index.QueryAsync(new IndexQuery
            {
                NamespaceId = NamespaceId,
                KeyPrefix = relativePath
            }, ct).ConfigureAwait(false);
            return page.Items.FirstOrDefault(e => e.ObjectKey == relativePath)?.ContentHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反查对象哈希失败 Path={Path}", relativePath);
            return null;
        }
    }

    /// <summary>
    /// 统一读取入口（带 Redis 缓存）：先查缓存（Base64），未命中再从 FileBox 按 ContentHash 读整对象并惰性回填。
    /// </summary>
    private async Task<(byte[]? Content, NotFileStorageResponse Response)> ReadContentCachedAsync(string relativePath)
    {
        var cacheKey = CacheKey(relativePath);
        try
        {
            var cached = await _redis.StringGetAsync(cacheKey).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(cached))
            {
                var bytes = Convert.FromBase64String(cached);
                return (bytes, Success(relativePath, bytes, ComputeThumbHash(bytes)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "读取文件内容缓存失败 Key={Key}", cacheKey);
        }

        try
        {
            var hash = await ResolveHashAsync(relativePath).ConfigureAwait(false);
            if (string.IsNullOrEmpty(hash))
            {
                _logger.LogWarning("读取文件失败（对象不存在） Path={Path}", relativePath);
                return (null, Failure($"文件不存在：{relativePath}"));
            }

            var getResult = await _get.ExecuteAsync(new GetObjectCommand
            {
                ContentHash = hash,
                NamespaceId = NamespaceId,
                Offset = 0,
                Length = null
            }, CancellationToken.None).ConfigureAwait(false);

            using var content = getResult.Content;
            if (content is null)
            {
                _logger.LogWarning("读取文件失败（对象不可读） Path={Path}", relativePath);
                return (null, Failure($"文件不存在：{relativePath}"));
            }

            var bytes = ReadAllBytes(content);
            await BackfillCacheAsync(cacheKey, bytes).ConfigureAwait(false);
            return (bytes, Success(relativePath, bytes, hash));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败 FileRelativePath={Path}", relativePath);
            return (null, Failure("读取文件失败"));
        }
    }

    /// <summary>将流读为字节数组。</summary>
    private static byte[] ReadAllBytes(Stream stream)
    {
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var buf))
            return buf.ToArray();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>缓存命中时无从取 FileBox 哈希，回退用本地 SHA256 概要作为内容哈希（仅用于缓存响应展示）。</summary>
    private static string ComputeThumbHash(byte[] bytes)
        => HashHelper.ComputeHash(bytes, AlgorithmType.SHA256);

    /// <summary>读取文件内容（字节）。</summary>
    public async Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        return (content!, response);
    }

    /// <summary>
    /// 流式获取文件内容：命中 Redis 缓存时直取字节；未命中则从 FileBox 读整对象后回填。
    /// 内存流承接以维持上层流式下载契约（<c>/files</c> 的浏览器 Range 语义仍可用，但基于内存）。
    /// </summary>
    public async Task<(Stream? Content, NotFileStorageResponse Response)> GetContentStreamAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        if (content is null)
            return (null, response);
        return (new MemoryStream(content, writable: false), response);
    }

    /// <summary>保存文件（FileBox 覆盖写即重写索引条目，随后失效内容缓存）。</summary>
    public async Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request)
    {
        try
        {
            var putResult = await _put.ExecuteAsync(new PutObjectCommand
            {
                NamespaceId = NamespaceId,
                ObjectKey = request.FileRelativePath,
                Content = new MemoryStream(request.FileContent, writable: false)
            }, CancellationToken.None).ConfigureAwait(false);

            if (!putResult.Succeeded)
            {
                _logger.LogError("保存文件失败 FileRelativePath={Path} Error={Error}",
                    request.FileRelativePath, putResult.Error);
                return Failure(putResult.Error ?? "保存文件失败");
            }

            // 覆盖写后失效旧内容缓存，防止误读脏数据
            await InvalidateCacheAsync(request.FileRelativePath).ConfigureAwait(false);

            return Success(request.FileRelativePath, request.FileContent, putResult.ContentHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存文件失败 FileRelativePath={Path}", request?.FileRelativePath);
            return Failure("保存文件失败");
        }
    }

    /// <summary>删除文件：反查哈希后走 FileBox 删除用例（逻辑删除），随后失效内容缓存。</summary>
    public async Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath)
    {
        try
        {
            var hash = await ResolveHashAsync(fileRelativePath).ConfigureAwait(false);
            if (string.IsNullOrEmpty(hash))
            {
                _logger.LogWarning("删除文件失败（对象不存在） Path={Path}", fileRelativePath);
                return Failure($"文件不存在：{fileRelativePath}");
            }

            await _delete.ExecuteAsync(new DeleteObjectCommand
            {
                ContentHash = hash,
                NamespaceId = NamespaceId
            }, CancellationToken.None).ConfigureAwait(false);

            await InvalidateCacheAsync(fileRelativePath).ConfigureAwait(false);
            return new NotFileStorageResponse { Success = true, FullPath = fileRelativePath };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文件失败 FileRelativePath={Path}", fileRelativePath);
            return Failure("删除文件失败");
        }
    }

    /// <summary>检查文件是否存在（底层异常一律视为不存在，避免上传流程被存储异常打断）。</summary>
    public async Task<bool> ExistsAsync(string fileRelativePath)
    {
        var hash = await ResolveHashAsync(fileRelativePath).ConfigureAwait(false);
        return !string.IsNullOrEmpty(hash);
    }
}