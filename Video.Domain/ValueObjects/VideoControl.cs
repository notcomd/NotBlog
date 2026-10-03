namespace Video.Domain.ValueObjects;

/// <summary>
/// 视频控制值对象 — 承载视频的可见性、作者权限、弹幕开关、删除标记与保护时间等控制项。
/// </summary>
public record VideoControl
{
    private VideoControl()
    {
        VideoDelete = false;
        VideoDisplay = false;
        AuthorVideo = AuthorVideo.VideoPublic;
        BarrageControl = BarrageControl.BarrageOn;
        TimeSpace = new();
        VideoProtectedTime = VideoProtectedTime.Create(null, null);
    }

    /// <summary>创建默认的视频控制值对象。</summary>
    public static VideoControl VideoControlBuilder()
    {
        return new VideoControl();
    }

    /// <summary>是否已删除</summary>
    public bool VideoDelete { get; private set; }

    /// <summary>是否公开展示</summary>
    public bool VideoDisplay { get; private set; }

    /// <summary>作者可见权限</summary>
    public AuthorVideo AuthorVideo { get; private set; }

    /// <summary>弹幕开关</summary>
    public BarrageControl BarrageControl { get; private set; }

    /// <summary>保护时间（可空）</summary>
    public VideoProtectedTime? VideoProtectedTime { get; private set; }

    /// <summary>时间戳</summary>
    public TimeSpace TimeSpace { get; private set; }


    /// <summary>以另一个控制对象整体覆盖当前控制项（保护时间非空时同步覆盖）。</summary>
    /// <param name="videoControl">源控制对象</param>
    public void ChangeByVideoController(VideoControl videoControl)
    {
        VideoDelete = videoControl.VideoDelete;
        VideoDisplay = videoControl.VideoDisplay;
        AuthorVideo = videoControl.AuthorVideo;
        BarrageControl = videoControl.BarrageControl;
        if (videoControl.VideoProtectedTime is not null)
            VideoProtectedTime = videoControl.VideoProtectedTime;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }



    /// <summary>设置弹幕开关。</summary>
    public void Barrage(BarrageControl barrageControl)
    {
        BarrageControl = barrageControl;
    }

    /// <summary>设置删除标记。</summary>
    public void Delete(bool delete)
    {
        VideoDelete = delete;
    }

    /// <summary>发布展示（未展示时置为展示）。</summary>
    public void Push()
    {
        if(!IsVideoDisplay())
            VideoDisplay = true;
    }

    /// <summary>设置作者可见权限。</summary>
    public void Author(AuthorVideo author)
    {
        AuthorVideo = author;
    }

    /// <summary>设置保护时间范围。</summary>
    public void SetProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        VideoProtectedTime = VideoProtectedTime.Create(startTime, endTime);
    }


    /// <summary>是否已删除。</summary>
    public bool IsVideoDelete()
    {
        return VideoDelete;
    }

    /// <summary>是否公开展示。</summary>
    public bool IsVideoDisplay()
    {
        return VideoDisplay;
    }

}