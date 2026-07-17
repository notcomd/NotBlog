﻿namespace Notcomd.Evenbus;

/// <summary>
/// Outbox 配置选项
/// </summary>
public class OutboxOptions
{
    /// <summary>扫描间隔（毫秒），默认 5000ms</summary>
    public int PollingIntervalMs { get; set; } = 5000;

    /// <summary>每次批量处理的最大消息数</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>消息保留天数（已发送成功）</summary>
    public int RetentionDays { get; set; } = 7;
}