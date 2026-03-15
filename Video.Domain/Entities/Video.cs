using Notcomd.Token.JWT;
using Video.Domain.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public class Videos : Entity, IAggregateRoot
{
    private Videos()
    {
        Affiliated = [];
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        VideoControl = VideoControl.VideoControlBuilder();
        VideoTags = [];
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


    /// <summary>
    ///     视频主键id
    /// </summary>
    public Guid VideoGuid { get; init; }

    /// <summary>
    ///     所属用户
    /// </summary>
    public HashSet<Guid> Affiliated;

    /// <summary>
    ///     封面的uri
    /// </summary>
    public Uri VideoCover { get; private set; } = null!;

    /// <summary>
    ///     标题
    /// </summary>
    public string VideoName { get; private set; } = null!;

    /// <summary>
    ///     简介
    /// </summary>
    public string BriefIntroduction { get; private set; }

    /// <summary>
    ///     标签
    /// </summary>
    public HashSet<string> VideoTags { get; private set; }

    /// <summary>
    ///     视频的uri
    /// </summary>
    public Uri VideoFileUri { get; private set; } = null!;

    /// <summary>
    ///  视频的nvid
    /// </summary>
    public string VideoNvid { get; init; }


    /// <summary>
    ///     评论点赞等的数量
    /// </summary>
    public VideoQuote VideoQuote { get; private set; }

    /// <summary>
    ///     弹幕
    /// </summary>
    public List<VideoBarrage>? VideoBarrageList { get; private set; }

    /// <summary>
    ///     视频评论
    /// </summary>
    public List<VideoReview>? VideoReviews { get; }

    /// <summary>
    ///     创建时间
    /// </summary>
    public TimeSpace TimeSpace { get; private set; }

    /// <summary>
    ///     权限管理
    /// </summary>
    public VideoControl VideoControl { get; private set; }


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

    public void AddByVideoBarrage(List<VideoBarrage>? videoBarrage)
    {
        if (videoBarrage is null) return;
        VideoBarrageList?.AddRange(videoBarrage);
    }


    public void AddByVideoReview(Guid userGuid, string? videoReviewBody)
    {
        VideoReviews!.Add(VideoReview.CreateVideoReview(VideoGuid, userGuid, videoReviewBody));
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
        HashSet<string> videoTags)
    {
        VideoName = videoName;
        BriefIntroduction = briefIntroduction;
        VideoCover = videoCover;
        VideoFileUri = videoFileUri;
        AddVideoTags(videoTags);
    }
}