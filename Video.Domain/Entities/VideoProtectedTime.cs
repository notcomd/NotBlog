namespace Video.Domain.Entities;

/// <summary>
/// 管理时间
/// </summary>
/// <param name="StartTime">有效开始时间</param>
/// <param name="EndTime">有效结束时间</param>
public class  VideoProtectedTime
{
    private VideoProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime < startTime) throw new ArgumentException("结束时间不能小于开始时间");
        StartTime = startTime;
        EndTime = endTime;
    }


    public VideoProtectedTime()
    {
        StartTime = DateTimeOffset.MinValue;
        EndTime = DateTimeOffset.MinValue;
    }

    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }


    public void SetStartTime(DateTimeOffset startTime)
    {
        StartTime = startTime;
    }

    public void SetEndTime(DateTimeOffset endTime)
    {
        EndTime = endTime;
    }

    public static VideoProtectedTime Create(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return new VideoProtectedTime(startTime, endTime);
    }

    public static VideoProtectedTime? Create(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        // null 入参视为未设置（MinValue），与 VideoControl 默认构造（null, null）兼容；
        // 否则 VideoControl.VideoControlBuilder() 在构造 Phase 即抛 Nullable Must have a value。
        var start = startTime ?? DateTimeOffset.MinValue;
        var end = endTime ?? DateTimeOffset.MinValue;

        if (start == DateTimeOffset.MinValue && end == DateTimeOffset.MinValue) return null;

        return new VideoProtectedTime(start, end);
    }
}