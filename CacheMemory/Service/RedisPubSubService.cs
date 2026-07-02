using System.Text.Json;
using CacheMemory.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CacheMemory.Service;

/// <summary>
/// Redis 发布/订阅服务。
/// 提供消息发布和订阅能力，支持多个订阅处理器的注册与注销。
/// </summary>
public class RedisPubSubService : IAsyncDisposable
{
    private readonly IRedisCacheService _cacheService;
    private readonly Dictionary<string, List<Action<string, string>>> _handlers = new();
    private readonly object _lock = new();
    private readonly ILogger<RedisPubSubService> _logger;
    private volatile bool _isDisposed;

    public RedisPubSubService(IRedisCacheService cacheService, ILogger<RedisPubSubService>? logger = null)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? NullLogger<RedisPubSubService>.Instance;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        lock (_lock)
        {
            foreach (var channel in _handlers.Keys.ToList())
            {
                _cacheService.Unsubscribe(channel);
            }

            _handlers.Clear();
        }
    }

    /// <summary>
    /// 向指定频道发布消息。
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <param name="message">消息内容</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>接收消息的订阅者数量</returns>
    public virtual async Task<long> PublishAsync(string channel, string message, CancellationToken ct = default)
        => await _cacheService.PublishAsync(channel, message, ct).ConfigureAwait(false);

    /// <summary>
    /// 同步发布消息。
    /// </summary>
    public virtual long Publish(string channel, string message)
        => _cacheService.Publish(channel, message);

    /// <summary>
    /// 订阅频道。当收到消息时，调用传入的 handler。
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <param name="handler">消息处理委托</param>
    /// <param name="ct">取消令牌</param>
    public virtual async Task SubscribeAsync(string channel, Action<string, string> handler,
        CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (!_handlers.ContainsKey(channel))
                _handlers[channel] = new List<Action<string, string>>();
            _handlers[channel].Add(handler);
        }

        await _cacheService.SubscribeAsync(channel, (ch, msg) =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(ch, out var handlers))
                {
                    foreach (var h in handlers)
                    {
                        try
                        {
                            h(ch, msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Pub/Sub 处理异常，Channel: {Channel}", ch);
                        }
                    }
                }
            }
        }, ct).ConfigureAwait(false);

        _logger.LogInformation("已订阅频道: {Channel}", channel);
    }

    /// <summary>
    /// 取消订阅频道。会移除此频道上的所有处理器。
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <param name="ct">取消令牌</param>
    public virtual async Task UnsubscribeAsync(string channel, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _handlers.Remove(channel);
        }

        await _cacheService.UnsubscribeAsync(channel, ct).ConfigureAwait(false);
        _logger.LogInformation("已取消订阅频道: {Channel}", channel);
    }

    /// <summary>
    /// 发布序列化的对象到频道。
    /// </summary>
    public virtual async Task<long> PublishObjectAsync<T>(string channel, T obj, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(obj);
        return await PublishAsync(channel, json, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 同步发布序列化对象。
    /// </summary>
    public virtual long PublishObject<T>(string channel, T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return Publish(channel, json);
    }
}