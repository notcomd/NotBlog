using Commons.SeedWork;
using Video.Domain.ValueObjects;
using Video.Domain.Events;

namespace Video.Domain.Entities;

/// <summary>
/// 视频实体
/// </summary>
public class Videos : Entity<int>, IAggregateRoot
{
    public Guid VideoGuid { get; init; }

    public HashSet<Guid> Affiliated { get; private set; }

    public Uri VideoCover { get; private set; } = null!;

    public string VideoName { get; private set; } = null!;

    public string BriefIntroduction { get; private set; } = null!;

    public HashSet<string> VideoTags { get; private set; }

    public Uri VideoFileUri { get; private set; } = null!;

    public string VideoNvid { get; init; } = null!;

    public bool IsDeleted { get; private set; }

    public TimeSpace TimeSpace { get; private set; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }

    public ICollection<VideoBarrage>? VideoBarrageList { get; private set; }

    public ICollection<VideoReview>? VideoReviews { get; private set; }
    private Videos()
    {
        Affiliated = [];
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        VideoControl = VideoControl.VideoControlBuilder();
        VideoTags = [];
        VideoBarrageList = [];
        VideoReviews = [];
        IsDeleted = false;
        TimeSpace = new TimeSpace(DateTime.UtcNow, DateTime.UtcNow);
    }

    public Videos(HashSet<Guid> affiliatedAuthorizes, string videoName, Uri videoCover
        , Uri videoFileUri, string briefIntroduction, HashSet<string> videoTags) : this()
    {
        AddByUser(affiliatedAuthorizes);
        VideoName = videoName;
        VideoCover = videoCover;
        VideoFileUri = videoFileUri;
        BriefIntroduction = briefIntroduction;
        AddVideoTags(videoTags);
        AddDomainEvent(new UploadVideoDomainEvent(VideoGuid, videoName, videoFileUri.ToString()));
    }


    private void AddByUser(HashSet<Guid> affiliated)
    {
        Affiliated = affiliated ?? throw new ArgumentNullException($"{affiliated}不为空");
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void SetProtectedTime(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (VideoControl.AuthorVideo != AuthorVideo.VideoProtected)
            throw new InvalidOperationException("当前视频权限不是视频保护");
        if (endTime < startTime)
            throw new AggregateException("结束时间不能小于开始时间");
        VideoControl.SetProtectedTime(startTime, endTime);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void AddByVideoBarrage(VideoBarrage videoBarrage)
    {
        if (videoBarrage is null) return;
        VideoBarrageList?.Add(videoBarrage);
    }


    /// <summary>
    /// 添加视频评论
    /// </summary>
    /// <param name="userGuid">用户Guid</param>
    /// <param name="rootReview">是否为回复</param>
    /// <param name="videoReviewBody">评论内容</param>
    /// <param name="videoImage">图片</param>
    public void AddByVideoReview(Guid userGuid, Guid? rootReview, string? videoReviewBody, List<VideoImage>? videoImage)
    {
        VideoReviews!.Add(new VideoReview(VideoGuid, userGuid, rootReview, videoReviewBody,
            videoImage is { Count: < 9 and > 0 } ? videoImage : null));
    }

    /// <summary>
    /// 添加视频标签
    /// </summary>
    /// <param name="videoTags">视频标签</param>
    private void AddVideoTags(HashSet<string> videoTags)
    {
        VideoTags = videoTags ?? throw new ArgumentNullException($"{videoTags}不为空");
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 设置视频保护结束时间
    /// </summary>
    /// <param name="timeOffset">结束时间</param>
    /// <exception cref="ArgumentException">结束时间不能小于开始时间</exception>
    public void SetProtectedEnd(DateTimeOffset timeOffset)
    {
        if (VideoControl.VideoProtectedTime!.StartTime >= timeOffset)
            throw new ArgumentException("结束时间不能小于开始时间");
        VideoControl.VideoProtectedTime!.SetEndTime(timeOffset);
    }

    /// <summary>
    /// 更新视频信息
    /// </summary>
    /// <param name="videoName">视频名称</param>
    /// <param name="briefIntroduction">视频简介</param>
    /// <param name="videoCover">视频封面</param>
    /// <param name="videoFileUri">视频文件Uri</param>
    /// <param name="videoTags">视频标签</param>
    /// <param name="videoControl">视频控制权限</param>
    public void UpDataVideo(string videoName, string briefIntroduction, Uri videoCover, Uri videoFileUri,
        HashSet<string> videoTags, VideoControl videoControl)
    {
        VideoName = videoName;
        BriefIntroduction = briefIntroduction;
        VideoCover = videoCover;
        VideoFileUri = videoFileUri;
        AddVideoTags(videoTags);
        VideoControl.ChangeByVideoController(videoControl);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 更新视频引用
    /// </summary>
    /// <param name="videoQuote">视频引用</param>
    public void ChangeByQuote(VideoQuote videoQuote)
    {
        VideoQuote = videoQuote;
    }

    /// <summary>
    /// 删除视频
    /// </summary>
    public void DeleteVideo()
    {
        IsDeleted = true;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void VideoControlChangeByVideoController(VideoControl videoControl)
    {
        VideoControl.ChangeByVideoController(videoControl);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

}