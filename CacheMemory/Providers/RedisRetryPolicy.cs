using System.Net.Sockets;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;

namespace CacheMemory.Providers;

/// <summary>
/// Redis 操作的指数退避重试策略实现。
/// 对 transient 类型的 Redis 异常（如连接超时、Socket 错误等）自动重试，
/// 对明确的业务错误（如类型不匹配）则不重试直接抛出。
/// </summary>
public class RedisRetryPolicy : IRedisRetryPolicy
{
    /// <summary>
    /// 已知的不可重试异常消息关键词。
    /// </summary>
    private static readonly HashSet<string> NonRetryableMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "WRONGTYPE",
        "NOSCRIPT",
        "BUSYGROUP",
        "NOAUTH",
        "WRONGPASS",
        "READONLY",
        "MOVED", // 集群重定向需特殊处理，不应简单重试
    };

    private readonly ILogger<RedisRetryPolicy> _logger;
    private readonly RetryOptions _options;

    public RedisRetryPolicy(RetryOptions options, ILogger<RedisRetryPolicy>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<RedisRetryPolicy>.Instance;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (var attempt = 0;; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (attempt < _options.MaxRetryCount && IsRetryable(ex))
            {
                var delay = _options.GetDelay(attempt);
                _logger.LogWarning(ex,
                    "Redis 操作失败，第 {Attempt}/{MaxRetry} 次重试，等待 {DelayMs}ms",
                    attempt + 1, _options.MaxRetryCount, delay.TotalMilliseconds);

                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw; // 取消令牌触发时直接抛出
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public T Execute<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        for (var attempt = 0;; attempt++)
        {
            try
            {
                return action();
            }
            catch (Exception ex) when (attempt < _options.MaxRetryCount && IsRetryable(ex))
            {
                var delay = _options.GetDelay(attempt);
                _logger.LogWarning(ex,
                    "Redis 操作失败（同步），第 {Attempt}/{MaxRetry} 次重试，等待 {DelayMs}ms",
                    attempt + 1, _options.MaxRetryCount, delay.TotalMilliseconds);

                Thread.Sleep(delay);
            }
        }
    }

    /// <inheritdoc />
    public void Execute(Action action)
    {
        Execute(() =>
        {
            action();
            return true;
        });
    }

    /// <summary>
    /// 判断异常是否可重试。
    /// Redis 连接异常、超时异常等通常可重试；数据类型错误等不可重试。
    /// </summary>
    private static bool IsRetryable(Exception ex)
    {
        // 不可重试的消息检查
        if (ex is RedisServerException rse && NonRetryableMessages.Any(m => rse.Message.Contains(m)))
            return false;

        // 可重试的异常类型
        return ex switch
        {
            RedisConnectionException => true,
            RedisTimeoutException => true,
            RedisCommandException => false, // 命令错误不应重试
            RedisException => true, // 其他 Redis 异常默认重试
            TimeoutException => true,
            IOException => true,
            SocketException => true,
            ObjectDisposedException => false,
            OperationCanceledException => false,
            _ => false // 未知异常不重试
        };
    }
}