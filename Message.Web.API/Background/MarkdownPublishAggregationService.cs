using CacheMemory.Core;
using Message.Web.API.Application.IntegrationEvents.EventHandlers;

namespace Message.Web.API.Background;

/// <summary>
///      Markdown 发布通知聚合服务：每 10 分钟扫描 Redis 聚合桶，
///      将窗口内同一接收者的多条「好友/关注者发布文章」合并为一条站内通知后发送。
///      <para>
///      幂等：先删桶再摘索引；崩溃窗口极小（通知可能重复一条，可接受）。
///      多实例：每桶处理锁（Redis SETNX + TTL 30s）防止并发重复聚合。
///      </para>
/// </summary>
public class MarkdownPublishAggregationService(
    IServiceScopeFactory scopeFactory,
    ILogger<MarkdownPublishAggregationService> logger) : BackgroundService
{
    /// <summary>处理周期 = 聚合窗口（与事件侧写入窗口一致：10 分钟）</summary>
    private static readonly TimeSpan Window = MarkdownCreatedIntegrationEventHandler.AggregateWindow;

    /// <summary>启动后首次执行延迟（等待 Redis 就绪）</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    /// <summary>单桶处理锁 TTL（防多实例并发聚合同一桶）</summary>
    private static readonly TimeSpan BucketLockTtl = TimeSpan.FromSeconds(30);

    /// <summary>聚合文案中最多列出的昵称数（超出显示「等」）</summary>
    private const int MaxNickSamples = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Window);
        try
        {
            do
            {
                await AggregateOnceAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Markdown 发布聚合任务已停止");
        }
    }

    private async Task AggregateOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var redis = scope.ServiceProvider.GetService<IRedisCacheService>();
        if (redis is null)
            return; // 无 Redis（本地模式/未配置）：事件侧已走降级直发，无需聚合

        var notificationRepo = scope.ServiceProvider.GetRequiredService<ITweetNotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var delivery = scope.ServiceProvider.GetRequiredService<MessageDeliveryService>();

        IEnumerable<string> members;
        try
        {
            members = await redis.SetMembersAsync(MarkdownCreatedIntegrationEventHandler.ReceiversIndexKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "读取 Markdown 发布聚合索引失败（下周期重试）");
            return;
        }

        foreach (var receiverIdStr in members)
        {
            if (!Guid.TryParse(receiverIdStr, out var receiverId))
            {
                await redis.SetRemoveAsync(MarkdownCreatedIntegrationEventHandler.ReceiversIndexKey, receiverIdStr, ct);
                continue;
            }

            var bucketKey = MarkdownCreatedIntegrationEventHandler.BucketKey(receiverId);
            var lockKey = $"{bucketKey}:lock";

            try
            {
                // 防多实例并发聚合同一桶（未抢到锁则留给其他实例）
                var locked = await redis.StringSetIfNotExistsAsync(
                    lockKey, Guid.NewGuid().ToString("N"), BucketLockTtl, ct);
                if (!locked)
                    continue;

                try
                {
                    var entries = await redis.HashGetAllAsync(bucketKey, ct);
                    if (entries.Count == 0)
                    {
                        // 桶已被处理/过期：仅清理索引（幂等跳过）
                        await redis.SetRemoveAsync(MarkdownCreatedIntegrationEventHandler.ReceiversIndexKey, receiverIdStr, ct);
                        continue;
                    }

                    var (title, content, refGuid) = BuildAggregatedCopy(entries);
                    var notify = TweetNotification.Create(
                        receiverId, NotificationType.MarkdownPublished, title, content,
                        refType: "Markdown", refGuid: refGuid);
                    await notificationRepo.AddAsync(notify);
                    await unitOfWork.SaveEntitiesAsync(ct);
                    await delivery.NotifyNotificationAsync(receiverId, notify.ToDto(), ct);

                    // 处理完成：先删桶再摘索引（崩溃窗口极小，通知最多重复一条）
                    await redis.KeyDeleteAsync(bucketKey, ct);
                    await redis.SetRemoveAsync(MarkdownCreatedIntegrationEventHandler.ReceiversIndexKey, receiverIdStr, ct);

                    logger.LogInformation("Markdown 发布聚合通知已发送：接收者 {Receiver}，聚合 {Count} 篇",
                        receiverId, entries.Count);
                }
                finally
                {
                    await redis.KeyDeleteAsync(lockKey, ct);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Markdown 发布聚合处理失败：接收者 {Receiver}", receiverId);
            }
        }
    }

    /// <summary>
    ///     由桶数据组装聚合通知文案（独立纯函数，public 便于单测）。
    ///     <para>value 格式：{文章名}|{发布者昵称}；key 格式：{发布者Guid:N}:{文章Guid:N}</para>
    /// </summary>
    public static (string Title, string Content, Guid RefGuid) BuildAggregatedCopy(
        IReadOnlyDictionary<string, string> entries)
    {
        var payloads = entries.Values.ToList();
        var fields = entries.Keys.ToList();

        var nicks = payloads
            .Select(ParseNick)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        var fileName = ParseFileName(payloads[^1]) ?? "新文章";
        var refGuid = ParseMarkdownGuid(fields[^1]);

        // 窗口内仅一位发布者：保持单条文案
        if (entries.Count == 1)
        {
            var nick = nicks.FirstOrDefault() ?? "一位好友";
            return ("好友/关注者发布了新文章", $"{nick} 发布了新文章《{fileName}》", refGuid);
        }

        // 多位发布者：聚合提醒
        var nickText = string.Join("、", nicks.Take(MaxNickSamples))
                       + (nicks.Count > MaxNickSamples ? " 等" : "");
        return ("好友/关注者动态",
            $"您的 {entries.Count} 位好友/关注者发布了新文章：{nickText}", refGuid);
    }

    private static string? ParseFileName(string payload)
    {
        var idx = payload.IndexOf('|');
        return idx < 0 ? payload : payload[..idx];
    }

    private static string? ParseNick(string payload)
    {
        var idx = payload.IndexOf('|');
        return idx < 0 ? null : payload[(idx + 1)..];
    }

    private static Guid ParseMarkdownGuid(string field)
    {
        var idx = field.LastIndexOf(':');
        return Guid.TryParse(field[(idx + 1)..], out var guid) ? guid : Guid.Empty;
    }
}