﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notcomd.Evenbus;

/// <summary>
/// Outbox 待发消息实体
/// </summary>
public class OutboxMessage
{
    protected OutboxMessage()
    {
    }

    public OutboxMessage(string eventType, object eventData, string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type cannot be empty", nameof(eventType));

        Id = Guid.CreateVersion7();
        EventType = eventType;
        EventData = JsonSerializer.Serialize(eventData);
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
        ProcessCount = 0;
        Status = OutboxStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string EventData { get; private set; } = null!;
    public string? CorrelationId { get; private set; }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>处理次数</summary>
    public int ProcessCount { get; private set; }

    /// <summary>最后错误信息</summary>
    public string? LastError { get; private set; }

    /// <summary>发送状态: Pending=0, Sent=1, Failed=2</summary>
    public OutboxStatus Status { get; private set; }

    /// <summary>发送时间</summary>
    public DateTimeOffset? SentAt { get; private set; }

    internal void MarkSent()
    {
        Status = OutboxStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    internal void IncrementFailure(string error)
    {
        ProcessCount++;
        LastError = error;
        Status = OutboxStatus.Failed;
    }
}

/// <summary>
/// Outbox 消息状态
/// </summary>
public enum OutboxStatus
{
    /// <summary>待发送</summary>
    Pending = 0,

    /// <summary>已发送</summary>
    Sent = 1,

    /// <summary>发送失败</summary>
    Failed = 2
}

/// <summary>
/// OutboxMessage EF Core 实体配置
/// </summary>
public class OutboxMessageTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(m => m.EventType).IsRequired().HasMaxLength(200);
        builder.Property(m => m.EventData).IsRequired().HasColumnType("jsonb");
        builder.Property(m => m.CorrelationId).HasMaxLength(100);
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(m => m.ProcessCount).HasDefaultValue(0);
        builder.Property(m => m.LastError).HasMaxLength(1000);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.SentAt);

        // 索引：按状态+创建时间查询待发消息
        builder.HasIndex(m => new { m.Status, m.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Status_CreatedAt");

        // 索引：清理过期数据
        builder.HasIndex(m => m.SentAt)
            .HasDatabaseName("IX_OutboxMessages_SentAt");
    }
}