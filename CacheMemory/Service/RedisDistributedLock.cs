using System.Collections.Concurrent;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheMemory.Service;

/// <summary>
/// Redis 分布式锁实现。
/// 基于 Redis SET NX + Lua 脚本提供安全、可重入的分布式锁机制。
/// 支持锁获取、自动续期、安全释放等操作。
/// </summary>
/// <remarks>
/// 可重入性：同一异步执行流（owner）内可对同一把锁重复获取，内部维护重入计数；
/// 释放时仅递减计数，计数归零才真正删除 Redis 中的锁键。
/// </remarks>
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

    /// <summary>
    /// 当前异步执行流的 owner 标识（AsyncLocal 保证仅在同一执行流内共享，且跨 await 保持一致）。
    /// </summary>
    private static readonly AsyncLocal<string?> _currentOwnerId = new();

    /// <summary>
    /// 进程内重入状态表：key = (lockKey, ownerId)，value = 锁 token 与重入计数。
    /// </summary>
    private static readonly ConcurrentDictionary<(string LockKey, string OwnerId), LockEntry> _reentrancyStates = new();

    private readonly IRedisCacheService _cacheService;
    private readonly ILogger<RedisDistributedLock> _logger;

    public RedisDistributedLock(IRedisCacheService cacheService, ILogger<RedisDistributedLock>? logger = null)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? NullLogger<RedisDistributedLock>.Instance;
    }

    /// <summary>
    /// 尝试获取分布式锁。返回一个 <see cref="LockHandle"/>，dispose 时自动释放。
    /// 同一执行流内对同一锁键的重复获取视为重入（计数 +1），不会再次执行 SET NX。
    /// </summary>
    /// <param name="lockKey">锁的键</param>
    /// <param name="expiry">锁的过期时间（建议设置合理值以防止死锁）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>获取成功返回 LockHandle；失败返回 null</returns>
    public virtual async Task<LockHandle?> AcquireAsync(string lockKey, TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var ownerId = GetOwnerId();
        var stateKey = (lockKey, ownerId);

        // 重入路径：同一 owner 已持有该锁，且 Redis 中的锁仍由本 owner 持有（未过期、未被抢占）
        if (_reentrancyStates.TryGetValue(stateKey, out var entry)
            && await _cacheService.StringGetAsync(lockKey, cancellationToken).ConfigureAwait(false) == entry.LockValue)
        {
            Interlocked.Increment(ref entry.Count);
            _logger.LogDebug("重入获取分布式锁成功: {LockKey}，当前重入次数: {Count}", lockKey, entry.Count);
            return new LockHandle(lockKey, entry.LockValue, this);
        }

        var lockValue = GenerateLockValue();
        var acquired = await _cacheService.AcquireLockAsync(lockKey, lockValue, expiry, cancellationToken)
            .ConfigureAwait(false);

        if (!acquired)
        {
            _logger.LogDebug("获取分布式锁失败: {LockKey}", lockKey);
            return null;
        }

        // 索引器赋值：覆盖可能残留的陈旧条目（Redis 中锁键已过期/被抢占时）
        _reentrancyStates[stateKey] = new LockEntry(lockValue);
        _logger.LogDebug("获取分布式锁成功: {LockKey}", lockKey);
        return new LockHandle(lockKey, lockValue, this);
    }

    /// <summary>
    /// 尝试获取分布式锁（同步版本）。
    /// </summary>
    public virtual LockHandle? Acquire(string lockKey, TimeSpan expiry)
    {
        var ownerId = GetOwnerId();
        var stateKey = (lockKey, ownerId);

        if (_reentrancyStates.TryGetValue(stateKey, out var entry)
            && _cacheService.StringGet(lockKey) == entry.LockValue)
        {
            Interlocked.Increment(ref entry.Count);
            _logger.LogDebug("重入获取分布式锁成功: {LockKey}，当前重入次数: {Count}", lockKey, entry.Count);
            return new LockHandle(lockKey, entry.LockValue, this);
        }

        var lockValue = GenerateLockValue();
        var acquired = _cacheService.AcquireLock(lockKey, lockValue, expiry);

        if (!acquired)
        {
            _logger.LogDebug("同步获取分布式锁失败: {LockKey}", lockKey);
            return null;
        }

        _reentrancyStates[stateKey] = new LockEntry(lockValue);
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
    /// 重入计数大于 0 时仅递减计数，计数归零才真正删除 Redis 锁键。
    /// </summary>
    internal virtual async Task<bool> ReleaseAsync(string lockKey, string lockValue, CancellationToken ct = default)
    {
        var stateKey = (lockKey, GetOwnerId());

        if (_reentrancyStates.TryGetValue(stateKey, out var entry) && entry.LockValue == lockValue)
        {
            var remaining = Interlocked.Decrement(ref entry.Count);
            if (remaining > 0)
            {
                _logger.LogDebug("重入释放分布式锁（剩余重入次数: {Count}）: {LockKey}", remaining, lockKey);
                return true;
            }

            _reentrancyStates.TryRemove(stateKey, out _);
        }

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
        var stateKey = (lockKey, GetOwnerId());

        if (_reentrancyStates.TryGetValue(stateKey, out var entry) && entry.LockValue == lockValue)
        {
            var remaining = Interlocked.Decrement(ref entry.Count);
            if (remaining > 0)
            {
                _logger.LogDebug("重入释放分布式锁（剩余重入次数: {Count}）: {LockKey}", remaining, lockKey);
                return true;
            }

            _reentrancyStates.TryRemove(stateKey, out _);
        }

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

    /// <summary>
    /// 获取当前异步执行流的 owner 标识；不存在时惰性创建。
    /// </summary>
    private static string GetOwnerId()
        => _currentOwnerId.Value ??= $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    /// <summary>
    /// 进程内锁状态条目。
    /// </summary>
    private sealed class LockEntry
    {
        public LockEntry(string lockValue)
        {
            LockValue = lockValue;
        }

        /// <summary>写入 Redis 的锁 token（首次获取时生成）。</summary>
        public string LockValue { get; }

        /// <summary>重入计数。</summary>
        public int Count;
    }
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