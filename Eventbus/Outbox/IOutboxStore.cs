﻿namespace Notcomd.EventBus.Outbox;

/// <summary>
/// Outbox 消息存储接口（分布式事务模式）
/// 
/// 使用方式:
///   1. 在业务事务中调用 StoreAsync() 将消息写入数据库
///   2. OutboxPublisher 后台服务定期扫描并发送
///   3. 发送成功后标记为 Sent
/// </summary>
public interface IOutboxStore
{
    /// <summary>
    /// 存储消息到 Outbox（在业务事务内调用）
    /// </summary>
    Task StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取待发送的消息批次
    /// </summary>
    Task<List<OutboxMessage>> GetPendingBatchAsync(int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记消息发送成功
    /// </summary>
    Task MarkAsSentAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记消息发送失败（增加重试计数）
    /// </summary>
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的已发送消息
    /// </summary>
    Task CleanupExpiredAsync(int retentionDays, CancellationToken cancellationToken = default);
}