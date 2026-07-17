using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notcomd.Evenbus.Core;

namespace Notcomd.Evenbus;

/// <summary>
/// Outbox 消息发布后台服务
/// 
/// 定期扫描 Outbox 表中的待发消息，反序列化为 IntegrationEvent 并通过 EventBus 发布，
/// 实现分布式事务的最终一致性。
/// </summary>
public class OutboxPublisher<TDbContext> : BackgroundService where TDbContext : DbContext
{
    private readonly ILogger<OutboxPublisher<TDbContext>> _logger;
    private readonly IOptionsMonitor<OutboxOptions> _options;
    private readonly IServiceProvider _serviceProvider;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        IOptionsMonitor<OutboxOptions> options,
        ILogger<OutboxPublisher<TDbContext>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[Evenbus-Outbox] 后台发布服务已启动，扫描间隔: {Interval}ms",
            _options.CurrentValue.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var eventBus = scope.ServiceProvider.GetService<IEventBus>();
                var subscriptionInfo = scope.ServiceProvider.GetService<EventBusSubscriptionInfo>();

                // 1. 获取待发批次
                var batch = await outboxStore.GetPendingBatchAsync(
                    _options.CurrentValue.BatchSize, stoppingToken).ConfigureAwait(false);

                if (batch.Count > 0 && eventBus != null && subscriptionInfo != null)
                {
                    foreach (var message in batch)
                    {
                        try
                        {
                            // 2. 从 EventBusSubscriptionInfo 查找事件类型
                            if (!subscriptionInfo.EventTypes.TryGetValue(message.EventType, out var eventType))
                            {
                                _logger.LogWarning("[Evenbus-Outbox] 未找到事件类型: {EventType}", message.EventType);
                                await outboxStore.MarkAsSentAsync(message.Id, stoppingToken)
                                    .ConfigureAwait(false);
                                continue;
                            }

                            // 3. 反序列化为 IntegrationEvent
                            var eventData = JsonSerializer.Deserialize(
                                message.EventData, eventType, subscriptionInfo.JsonSerializerOptions);

                            if (eventData is IntegrationEvent integrationEvent)
                            {
                                await eventBus.PublishAsync(integrationEvent).ConfigureAwait(false);
                            }

                            await outboxStore.MarkAsSentAsync(message.Id, stoppingToken)
                                .ConfigureAwait(false);

                            _logger.LogDebug("[Evenbus-Outbox] 消息已发送: Id={Id}, Event={Event}",
                                message.Id, message.EventType);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "[Evenbus-Outbox] 消息发送失败（将重试）: Id={Id}, Event={Event}",
                                message.Id, message.EventType);
                            await outboxStore.MarkAsFailedAsync(message.Id, ex.Message, stoppingToken)
                                .ConfigureAwait(false);
                        }
                    }
                }

                // 4. 清理过期消息
                try
                {
                    await outboxStore.CleanupExpiredAsync(
                        _options.CurrentValue.RetentionDays, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[Evenbus-Outbox] 清理过期消息失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Evenbus-Outbox] 扫描周期出错");
            }

            await Task.Delay(_options.CurrentValue.PollingIntervalMs, stoppingToken)
                .ConfigureAwait(false);
        }
    }
}
