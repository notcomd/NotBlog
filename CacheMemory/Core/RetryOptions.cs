namespace CacheMemory.Core;

/// <summary>
/// Redis 操作重试策略的配置选项。
/// 支持指数退避算法，所有参数均可配置。
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// 最大重试次数（不含首次调用）。默认值为 3。
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 基础退避间隔（毫秒）。每次重试的等待时间将以指数方式增长。默认值为 100ms。
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// 最大退避间隔（毫秒）。当指数增长超过此值时将被截断。默认值为 30000ms（30秒）。
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 30_000;

    /// <summary>
    /// 退避倍数。每次重试后延迟 = BaseDelay * Multiplier^attempt。默认值为 2.0。
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// 是否在重试时添加随机抖动（Jitter），以避免惊群效应。默认启用。
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// 计算第 <paramref name="attempt"/> 次重试（从0开始）的等待时间。
    /// </summary>
    /// <param name="attempt">当前重试次数（0-indexed）</param>
    /// <returns>等待时间</returns>
    public TimeSpan GetDelay(int attempt)
    {
        var delayMs = BaseDelayMilliseconds * Math.Pow(BackoffMultiplier, attempt);
        delayMs = Math.Min(delayMs, MaxDelayMilliseconds);

        if (UseJitter)
        {
            // 添加 ±25% 的随机抖动
            var jitter = Random.Shared.NextDouble() * 0.5 + 0.75; // 0.75 ~ 1.25
            delayMs *= jitter;
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }
}