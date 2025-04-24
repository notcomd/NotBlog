namespace Video.Domain.Entities;

/// <summary>
///     管理时间
/// </summary>
/// <param name="StartTime">有效开始时间</param>
/// <param name="EndTime">有效结束时间</param>
public record VideoProtectedTime
{

    public VideoProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime < startTime)
        {
            throw new ArgumentException("结束时间不能小于开始时间");
        }
        StartTime = startTime;
        EndTime = endTime;
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

    public static VideoProtectedTime Crate(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return new VideoProtectedTime(startTime, endTime);
    }

    public static VideoProtectedTime? Crate(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        if (startTime == DateTimeOffset.MinValue && endTime == DateTimeOffset.MinValue) return null;

        return new VideoProtectedTime(startTime!.Value, endTime!.Value);
    }
}