using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheMemory.Service;

/// <summary>
/// Redis 分布式锁实现。
/// 基于 Redis SET NX + Lua 脚本提供安全、可重入的分布式锁机制。
/// 支持锁获取、自动续期、安全释放等操作。
/// </summary>
public class RedisDistributedLock
{
    /// <summary>
    /// 用于安全释放锁的 Lua 脚本（仅当 value 匹配时才删除）。
    /// </summary>
    private const string UnlockScript = @"
        if redis.call('GET', KEYS[1]) == ARGV[1] then
            return redis.call('DEL', KEYS[1])
        else
            return 0
        end";

    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<RedisDistributedLock> _logger;

    public RedisDistributedLock(IRedisCacheService cacheService, ILogger<RedisDistributedLock>? logger = null)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? NullLogger<RedisDistributedLock>.Instance;
    }

    /// <summary>
    /// 尝试获取分布式锁。返回一个 <see cref="LockHandle"/>，dispose 时自动释放。
    /// </summary>
    /// <param name="lockKey">锁的键</param>
    /// <param name="expiry">锁的过期时间（建议设置合理值以防止死锁）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回 LockHandle；失败返回 null</returns>
    public virtual async Task<LockHandle?> AcquireAsync(string lockKey, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var lockValue = GenerateLockValue();
        var acquired = await _cacheService.AcquireLockAsync(lockKey, lockValue, expiry, cancellationToken)
            .ConfigureAwait(false);

        if (!acquired)
        {
            _logger.LogDebug("获取分布式锁失败: {LockKey}", lockKey);
            return null;
        }

        _logger.LogDebug("获取分布式锁成功: {LockKey}", lockKey);
        return new LockHandle(lockKey, lockValue, this);
    }

    /// <summary>
    /// 尝试获取分布式锁（同步版本）。
    /// </summary>
    public virtual LockHandle? Acquire(string lockKey, TimeSpan expiry)
    {
        var lockValue = GenerateLockValue();
        var acquired = _cacheService.AcquireLock(lockKey, lockValue, expiry);

        if (!acquired)
        {
            _logger.LogDebug("同步获取分布式锁失败: {LockKey}", lockKey);
            return null;
        }

        return new LockHandle(lockKey, lockValue, this);
    }

    /// <summary>
    /// 尝试获取分布式锁，指定重试次数和间隔。
    /// </summary>
    /// <param name="lockKey">锁的键</param>
    /// <param name="expiry">锁过期时间</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="retryDelay">重试间隔</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回 LockHandle；失败返回 null</returns>
    public virtual async Task<LockHandle?> AcquireWithRetryAsync(
        string lockKey,
        TimeSpan expiry,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = retryDelay ?? TimeSpan.FromMilliseconds(100);

        for (var i = 0; i <= maxRetries; i++)
        {
            var handle = await AcquireAsync(lockKey, expiry, cancellationToken).ConfigureAwait(false);
            if (handle != null)
                return handle;

            if (i < maxRetries)
            {
                _logger.LogDebug("获取锁 {LockKey} 第 {Attempt}/{MaxRetries} 次失败，{DelayMs}ms 后重试",
                    lockKey, i + 1, maxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    /// <summary>
    /// 延长锁的过期时间。
    /// </summary>
    public virtual async Task<bool> ExtendAsync(string lockKey, string lockValue, TimeSpan additionalExpiry,
        CancellationToken ct = default)
        => await _cacheService.ExtendLockAsync(lockKey, lockValue, additionalExpiry, ct).ConfigureAwait(false);

    /// <summary>
    /// 释放锁（内部使用 Lua 脚本确保原子性）。
    /// </summary>
    internal virtual async Task<bool> ReleaseAsync(string lockKey, string lockValue, CancellationToken ct = default)
    {
        var released = await _cacheService.ReleaseLockAsync(lockKey, lockValue, ct).ConfigureAwait(false);
        if (!released)
        {
            _logger.LogWarning("释放分布式锁失败（锁可能已过期或被其他实例持有）: {LockKey}", lockKey);
        }

        return released;
    }

    /// <summary>
    /// 释放锁（同步版本）。
    /// </summary>
    internal virtual bool Release(string lockKey, string lockValue)
    {
        var released = _cacheService.ReleaseLock(lockKey, lockValue);
        if (!released)
            _logger.LogWarning("同步释放分布式锁失败: {LockKey}", lockKey);
        return released;
    }

    /// <summary>
    /// 生成唯一的锁持有者标识。
    /// </summary>
    private static string GenerateLockValue()
        => $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
}

/// <summary>
/// 分布式锁的持有句柄。实现 <see cref="IAsyncDisposable"/> 和 <see cref="IDisposable"/>，
/// 在释放时自动解锁。
/// </summary>
public sealed class LockHandle : IAsyncDisposable, IDisposable
{
    private readonly string _lockKey;
    private readonly RedisDistributedLock _lockManager;
    private readonly string _lockValue;
    private volatile bool _disposed;

    internal LockHandle(string lockKey, string lockValue, RedisDistributedLock lockManager)
    {
        _lockKey = lockKey;
        _lockValue = lockValue;
        _lockManager = lockManager;
    }

    /// <summary>
    /// 锁的键名。
    /// </summary>
    public string LockKey => _lockKey;

    /// <summary>
    /// 异步释放锁。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _lockManager.ReleaseAsync(_lockKey, _lockValue).ConfigureAwait(false);
    }

    /// <summary>
    /// 同步释放锁。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lockManager.Release(_lockKey, _lockValue);
    }

    /// <summary>
    /// 延长锁的过期时间。
    /// </summary>
    public async Task<bool> ExtendAsync(TimeSpan additionalExpiry, CancellationToken ct = default)
    {
        EnsureNotDisposed();
        return await _lockManager.ExtendAsync(_lockKey, _lockValue, additionalExpiry, ct).ConfigureAwait(false);
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LockHandle), $"锁 {_lockKey} 已被释放");
    }
}