using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class Videos : Entity, IAggregateRoot
{  
    public Guid VideoGuid { get; init; }

    public HashSet<Guid> Affiliated { get; private set; }

    public Uri VideoCover { get; private set; } = null!;

    public string VideoName { get; private set; } = null!;

    public string BriefIntroduction { get; private set; }

    public HashSet<string> VideoTags { get; private set; }
    
    public Uri VideoFileUri { get; private set; } = null!;
    
    public string VideoNvid { get; init; }
    
    public VideoQuote VideoQuote { get; private set; }
    
    public List<VideoBarrage>? VideoBarrageList { get; private set; }
    
    public List<VideoReview>? VideoReviews { get; }
    
    public TimeSpace TimeSpace { get; private set; }
    
    public VideoControl VideoControl { get; private set; }

    private Videos()
    {
        Affiliated = [];
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        VideoControl = VideoControl.VideoControlBuilder();
        VideoTags = [];
        VideoBarrageList = [];
        VideoReviews = [];
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

    private void AddVideoTags(HashSet<string> videoTags)
    {
        VideoTags = videoTags ?? throw new ArgumentNullException($"{videoTags}不为空");
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void SetProtectedEnd(DateTimeOffset timeOffset)
    {
        if (VideoControl.VideoProtectedTime!.StartTime >= timeOffset)
            throw new ArgumentException("结束时间不能小于开始时间");
        VideoControl.VideoProtectedTime!.SetEndTime(timeOffset);
    }

    public void UpDataVideo(string videoName, string briefIntroduction, Uri videoCover, Uri videoFileUri,
        HashSet<string> videoTags,VideoControl videoControl)
    {
        VideoName = videoName;
        BriefIntroduction = briefIntroduction;
        VideoCover = videoCover;
        VideoFileUri = videoFileUri;
        AddVideoTags(videoTags);
        VideoControl.ChangeByVideoController(videoControl);
    }
    
    public void ChangeByQuote(VideoQuote videoQuote)
    {
        VideoQuote = videoQuote;
    }
    
}