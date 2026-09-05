using Microsoft.Extensions.Logging;
using MohuTianchi.Lite;
using Notcomd.Token.JWT.Core;
using Notcomd.Token.JWT.Security;

namespace FileDev.Infrastructure.Service;

/// <summary>
/// 基于 Mohu-TianChi 精简对象存储（<see cref="IObjectStorage"/>）的 <see cref="INotFileStorageService"/> 适配器，
/// 作为 FileDev 的底层存储实现：内容寻址分片 + 块级去重 + 目录/索引 + 日志 + 安全，全部由 Lite 内核承载。
/// <para>
/// 本类按功能拆分为多个 partial 文件共存于同名目录：
/// 主文件（本文件）承载实例状态与文件 CRUD + 内容缓存；
/// <c>MohuObjectStorageService.Chunks.cs</c> 承载分片上传；<c>MohuObjectStorageService.Manifest.cs</c> 承载清单与分层；
/// <c>MohuObjectStorageService.Volumes.cs</c> 承载共享/租户数据卷与目录统计。
/// </para>
/// 注意：Lite 门面按整对象读写（ReadAsync 返回 byte[]），因此流式语义由内存流承担（下载仍支持浏览器 Range，但基于内存）。
/// </summary>
public sealed partial class MohuObjectStorageService : INotFileStorageService
{
    /// <summary>文件内容缓存在 Redis 的 key 前缀（Base64 存储，见 <see cref="IRedisCacheService"/>）。</summary>
    private const string ContentCachePrefix = "file:content:";

    private readonly IObjectStorage _storage;
    private readonly IRedisCacheService _redis;
    private readonly NotFileStorageOptions _config;
    private readonly ILogger<MohuObjectStorageService> _logger;

    /// <summary>构造适配器。</summary>
    /// <param name="storage">Lite 对象存储门面（单例，由应用装配创建）。</param>
    /// <param name="redis">统一 Redis 缓存服务（项目内唯一 Redis 访问入口）。</param>
    /// <param name="configOptions">文件存储配置（含下载缓存大小阈值与 TTL）。</param>
    /// <param name="logger">日志。</param>
    public MohuObjectStorageService(
        IObjectStorage storage,
        IRedisCacheService redis,
        IOptionsSnapshot<NotFileStorageOptions> configOptions,
        ILogger<MohuObjectStorageService> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>构造失败响应。</summary>
    private static NotFileStorageResponse Failure(string message) => new() { Success = false, ErrorMessage = message };

    /// <summary>构造成功响应。</summary>
    private static NotFileStorageResponse Success(string path, byte[] content) => new()
    {
        Success = true,
        FullPath = path,
        FileSize = content.Length,
        ActualHash = HashHelper.ComputeHash(content, AlgorithmType.SHA256)
    };

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

    /// <summary>
    /// 统一读取入口（带 Redis 缓存）：先查缓存（Base64），未命中再从 Lite 读整对象并惰性回填。
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
                return (bytes, Success(relativePath, bytes));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "读取文件内容缓存失败 Key={Key}", cacheKey);
        }

        try
        {
            var content = await _storage.ReadAsync(relativePath).ConfigureAwait(false);
            if (content is null)
            {
                _logger.LogWarning("读取文件失败（对象不存在） Path={Path}", relativePath);
                return (null, Failure($"文件不存在：{relativePath}"));
            }

            await BackfillCacheAsync(cacheKey, content).ConfigureAwait(false);
            return (content, Success(relativePath, content));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败 FileRelativePath={Path}", relativePath);
            return (null, Failure("读取文件失败"));
        }
    }

    /// <summary>读取文件内容（字节）。</summary>
    public async Task<(byte[] Content, NotFileStorageResponse Response)> GetContentAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        return (content!, response);
    }

    /// <summary>
    /// 流式获取文件内容：命中 Redis 缓存时直取字节；未命中则从 Lite 读整对象后回填。
    /// 内存流承接以维持上层流式下载契约（<c>/files</c> 的浏览器 Range 语义仍可用，但基于内存）。
    /// </summary>
    public async Task<(Stream? Content, NotFileStorageResponse Response)> GetContentStreamAsync(string fileRelativePath)
    {
        var (content, response) = await ReadContentCachedAsync(fileRelativePath).ConfigureAwait(false);
        if (content is null)
            return (null, response);
        return (new MemoryStream(content, writable: false), response);
    }

    /// <summary>保存文件（覆盖写后失效内容缓存），并回填 Lite 清单元数据。</summary>
    public async Task<NotFileStorageResponse> SaveAsync(NotFileStorageRequest request)
    {
        try
        {
            // ExpectedHash 前置校验（与旧实现一致，SHA256 统一）
            if (!string.IsNullOrWhiteSpace(request.ExpectedHash))
            {
                var actual = HashHelper.ComputeHash(request.FileContent, AlgorithmType.SHA256);
                if (!string.Equals(actual, request.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("保存文件哈希校验失败 FileRelativePath={Path} Expected={Expected} Actual={Actual}",
                        request.FileRelativePath, request.ExpectedHash, actual);
                    return Failure($"文件哈希校验失败，预期：{request.ExpectedHash}，实际：{actual}");
                }
            }

            await _storage.WriteAsync(request.FileRelativePath, request.FileContent,
                new WriteOptions { Overwrite = request.Overwrite }).ConfigureAwait(false);

            // 覆盖写后失效旧内容缓存，防止误读脏数据
            await InvalidateCacheAsync(request.FileRelativePath).ConfigureAwait(false);

            var response = new NotFileStorageResponse
            {
                Success = true,
                FullPath = request.FileRelativePath,
                FileSize = request.FileContent.Length,
                ActualHash = HashHelper.ComputeHash(request.FileContent, AlgorithmType.SHA256)
            };
            // 对齐 Lite 清单元数据（卷 ID / 分片数 / 更新时间）
            response.ContentHash = response.ActualHash;
            await ApplyManifestMetaAsync(response, request.FileRelativePath).ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存文件失败 FileRelativePath={Path}", request?.FileRelativePath);
            return Failure("保存文件失败");
        }
    }

    /// <summary>删除文件（对象不存在视为失败），对象删除后剔除内容缓存。</summary>
    public async Task<NotFileStorageResponse> DeleteAsync(string fileRelativePath)
    {
        try
        {
            var deleted = await _storage.DeleteAsync(fileRelativePath).ConfigureAwait(false);
            if (!deleted)
            {
                _logger.LogWarning("删除文件失败（对象不存在） Path={Path}", fileRelativePath);
                return Failure($"文件不存在：{fileRelativePath}");
            }

            // 对象删除后剔除内容缓存
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
        try
        {
            return await _storage.ExistsAsync(fileRelativePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查文件存在性失败 FileRelativePath={Path}", fileRelativePath);
            return false;
        }
    }
}