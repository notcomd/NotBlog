using Video.Domain.Events;
using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

/// <summary>
/// 视频观看历史记录聚合根。
/// 追踪用户观看视频的完整生命周期：开始 → 进度更新 → 结束。
///
/// 性能设计说明：
/// - 使用独立聚合根避免视频聚合膨胀
/// - 不持有视频/用户导航属性，避免 JOIN 查询
/// - 支持批量写入（每个观看会话一条记录）
/// </summary>
public class VideoHistory : Entity, IAggregateRoot
{
    /// <summary>历史记录唯一标识</summary>
    public Guid VideoHistoryGuid { get; init; }

    /// <summary>用户 GUID</summary>
    public Guid UserGuid { get; init; }

    /// <summary>被观看的视频 GUID</summary>
    public Guid VideoGuid { get; init; }

    /// <summary>观看开始时间</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>观看结束时间（null 表示仍在观看中）</summary>
    public DateTimeOffset? EndTime { get; private set; }

    /// <summary>实际观看时长（初始为 TimeSpan.Zero，结束时计算）</summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// 观看进度百分比（0.0 ~ 1.0）。
    /// 值来自客户端上报，非精确值。
    /// </summary>
    public double Progress { get; private set; }

    /// <summary>是否已观看完成（进度 ≥ 90% 视为完成）</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>最后上报的播放位置（秒），用于恢复播放</summary>
    public double LastPositionSeconds { get; private set; }

    /// <summary>审计时间戳</summary>
    public TimeSpace TimeSpace { get; private set; }

    // ── 构造器 ──

    private VideoHistory()
    {
        VideoHistoryGuid = Guid.CreateVersion7();
        TimeSpace = new TimeSpace(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 创建新的观看历史记录（点播开始）。
    /// </summary>
    /// <param name="userGuid">用户 GUID</param>
    /// <param name="videoGuid">视频 GUID</param>
    public VideoHistory(Guid userGuid, Guid videoGuid) : this()
    {
        if (userGuid == Guid.Empty)
            throw new ArgumentException("UserGuid must not be empty.", nameof(userGuid));
        if (videoGuid == Guid.Empty)
            throw new ArgumentException("VideoGuid must not be empty.", nameof(videoGuid));

        UserGuid = userGuid;
        VideoGuid = videoGuid;
        StartTime = DateTimeOffset.UtcNow;
        EndTime = null;
        Duration = TimeSpan.Zero;
        Progress = 0.0;
        LastPositionSeconds = 0.0;
        IsCompleted = false;

        AddDomainEvent(new VideoWatchStartedDomainEvent(
            VideoHistoryGuid, userGuid, videoGuid, StartTime));
    }

    // ── 行为方法 ──

    /// <summary>
    /// 更新观看进度（客户端定时上报）。
    /// 进度只能前进，不可倒退。
    /// </summary>
    /// <param name="progress">进度 0.0 ~ 1.0</param>
    /// <param name="positionSeconds">当前播放位置（秒）</param>
    public void UpdateProgress(double progress, double positionSeconds)
    {
        if (progress is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(progress),
                "Progress must be between 0.0 and 1.0.");

        if (positionSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(positionSeconds),
                "Position must not be negative.");

        // 进度只进不退
        if (progress > Progress)
        {
            Progress = progress;
            LastPositionSeconds = positionSeconds;
            TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);

            // 跨过半程或完成阈值时发布事件
            if (progress >= 0.9 && !IsCompleted)
                MarkCompleted();

            if (Progress is >= 0.25 or >= 0.50 or >= 0.75)
                AddDomainEvent(new VideoWatchProgressUpdatedDomainEvent(
                    VideoHistoryGuid, UserGuid, VideoGuid, Progress, TimeSpace.UpdateAt));
        }
    }

    /// <summary>
    /// 结束本次观看（用户暂停/退出/视频播放完毕）。
    /// 自动计算实际观看时长并锁定记录。
    /// </summary>
    /// <param name="endTime">结束时间（通常为 DateTimeOffset.UtcNow）</param>
    public void EndWatching(DateTimeOffset endTime)
    {
        if (EndTime.HasValue)
            throw new InvalidOperationException("This watching session has already ended.");

        if (endTime < StartTime)
            throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));

        EndTime = endTime;
        Duration = endTime - StartTime;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);

        if (IsCompleted)
            AddDomainEvent(new VideoWatchCompletedDomainEvent(
                VideoHistoryGuid, UserGuid, VideoGuid, Duration, EndTime.Value));
    }

    /// <summary>
    /// 标记为观看完成（由 UpdateProgress 在进度 ≥ 90% 时自动触发）。
    /// </summary>
    private void MarkCompleted()
    {
        IsCompleted = true;
    }

    // ── 验证 ──

    /// <summary>
    /// 验证实体数据的完整性和一致性。
    /// </summary>
    public bool IsValid(out string? error)
    {
        error = null;

        if (UserGuid == Guid.Empty)
        {
            error = "UserGuid is required.";
            return false;
        }

        if (VideoGuid == Guid.Empty)
        {
            error = "VideoGuid is required.";
            return false;
        }

        if (StartTime == default)
        {
            error = "StartTime is required.";
            return false;
        }

        if (Progress is < 0.0 or > 1.0)
        {
            error = $"Progress must be in [0.0, 1.0], got {Progress}.";
            return false;
        }

        if (EndTime.HasValue && EndTime.Value < StartTime)
        {
            error = "EndTime must be after StartTime.";
            return false;
        }

        if (Duration < TimeSpan.Zero)
        {
            error = "Duration must not be negative.";
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return $"VideoHistory [{VideoHistoryGuid}] User={UserGuid} Video={VideoGuid} " +
               $"Progress={Progress:P0} Completed={IsCompleted} Duration={Duration}";
    }
}
