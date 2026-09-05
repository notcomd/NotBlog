namespace FileDev.Domain.Entities;

using FileDev.Domain.Exception;

/// <summary>
/// 用户存储额度聚合根 — 记录每个用户可使用的存储文件额度（配额上限）与当前已用量。
/// <para>
/// 一个用户一条额度记录，以 <see cref="UserId"/> 为业务主键。用于写入前配额校验：
/// 相比现有"实时聚合文件表统计用量"（<c>GetTotalFileSizeByUserIdAsync</c>），
/// 本实体将额度上限与用量落库记账，支持按用户差异化初始额度与动态调整
/// （<see cref="AdjustQuota"/>），超限时抛出 <see cref="FileQuotaExceededException"/>。
/// </para>
/// </summary>
public class UserFileInfo : Entity<Guid>, IAggregateRoot
{
    /// <summary>用户 ID（业务主键，一个用户一条额度记录）。</summary>
    public Guid UserId { get; init; }

    /// <summary>存储额度上限（字节）。0 表示不限制。</summary>
    public long TotalQuotaBytes { get; private set; }

    /// <summary>当前已用存储额度（字节）。</summary>
    public long UsedBytes { get; private set; }

    /// <summary>剩余可用额度（字节；不限制时为 <see cref="long.MaxValue"/>）。</summary>
    public long AvailableBytes => TotalQuotaBytes <= 0 ? long.MaxValue : TotalQuotaBytes - UsedBytes;

    /// <summary>是否已达/超过额度上限（不限制时为 false）。</summary>
    public bool IsQuotaExceeded => TotalQuotaBytes > 0 && UsedBytes >= TotalQuotaBytes;

    /// <summary>最近一次记账/调整时间。</summary>
    public DateTimeOffset UpdateTime { get; private set; }

    private UserFileInfo()
    {
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>创建用户额度记录（新用户首次初始化，可带初始已用量）。</summary>
    /// <param name="userId">用户 ID。</param>
    /// <param name="totalQuotaBytes">额度上限（字节；0 表示不限制）。</param>
    /// <param name="usedBytes">初始已用额度（默认 0）。</param>
    public UserFileInfo(Guid userId, long totalQuotaBytes, long usedBytes = 0)
        : this()
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空", nameof(userId));
        if (totalQuotaBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(totalQuotaBytes), "额度上限不能为负数");
        if (usedBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(usedBytes), "已用额度不能为负数");
        if (totalQuotaBytes > 0 && usedBytes > totalQuotaBytes)
            throw new ArgumentException("初始已用额度不能超过额度上限");

        UserId = userId;
        TotalQuotaBytes = totalQuotaBytes;
        UsedBytes = usedBytes;
    }

    /// <summary>校验并占用额度（上传成功后记账）。超限抛出 <see cref="FileQuotaExceededException"/>。</summary>
    /// <param name="bytes">本次新增占用字节数。</param>
    public void Occupy(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "占用额度不能为负数");

        if (TotalQuotaBytes > 0 && UsedBytes + bytes > TotalQuotaBytes)
            throw new FileQuotaExceededException(
                $"用户存储配额不足：已用 {UsedBytes} 字节，需 {bytes} 字节，上限 {TotalQuotaBytes} 字节");

        UsedBytes += bytes;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>释放已占用的额度（删除文件/写入失败回退时调用）。</summary>
    /// <param name="bytes">本次释放字节数（不能超过当前已用量）。</param>
    public void Release(long bytes)
    {
        if (bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(bytes), "释放额度不能为负数");
        if (bytes > UsedBytes)
            throw new ArgumentException("释放额度不能超过当前已用额度");

        UsedBytes -= bytes;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    /// <summary>调整额度上限（适应额度变化，可升可降；0 表示不限制）。降额时若已用超出新额度则拒绝。</summary>
    /// <param name="newQuotaBytes">新的额度上限（字节）。</param>
    public void AdjustQuota(long newQuotaBytes)
    {
        if (newQuotaBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(newQuotaBytes), "额度上限不能为负数");
        if (newQuotaBytes > 0 && UsedBytes > newQuotaBytes)
            throw new ArgumentException($"降额失败：已用 {UsedBytes} 字节超出新额度 {newQuotaBytes} 字节");

        TotalQuotaBytes = newQuotaBytes;
        UpdateTime = DateTimeOffset.UtcNow;
    }
}
