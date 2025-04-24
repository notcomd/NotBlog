using Notcomd.Token.JWT;

namespace Video.Domain.Entities;

public class Videos : IAggregateRoot
{


    private Videos() {}

    public Videos(List<Affiliated> affiliatedAuthorizes, string videoName, Uri videoCover
        , Uri videoFileUri, string briefIntroduction, List<string> videoTags)
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
    public Guid VideoGuid { get; init; } = Guid.CreateVersion7();

    /// <summary>
    ///     所属用户
    /// </summary>
    public List<Affiliated> Affiliated { get; } = new();

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
    public List<string> VideoTags { get; } = new();

    /// <summary>
    ///     视频的uri
    /// </summary>
    public Uri VideoFileUri { get; private set; } = null!;

    /// <summary>
    ///     视频的nvid
    /// </summary>
    public string VideoNvid { get; init; } = NVIDGenerator.GenerateNvStyleIdWithUuid();

    /// <summary>
    ///     子视频或集数
    /// </summary>
    public List<Guid>? ChildVideosList { get; private set; } = new();

    /// <summary>
    ///     评论点赞等的数量
    /// </summary>
    public VideoQuote VideoQuote { get; private set; } = VideoQuote.VideoQuoteBuilder();

    /// <summary>
    ///     弹幕
    /// </summary>
    public List<VideoBarrage>? VideoBarrageList { get; } = new();

    /// <summary>
    ///     视频评论
    /// </summary>
    public List<VideoReview>? VideoReviews { get; } = new();

    /// <summary>
    ///     创建时间
    /// </summary>
    public TimeSpace TimeSpace { get; } = new(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    /// <summary>
    ///     权限管理
    /// </summary>
    public VideoControl VideoControl { get; } = VideoControl.VideoControlBuilder();


    private void AddByUser(List<Affiliated> affiliated)
    {
        if (affiliated == null) throw new ArgumentNullException($"{affiliated}不为空");
        Affiliated.AddRange(affiliated);
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

    public void AddByChild(List<Guid> childVideosList)
    {
        ChildVideosList = childVideosList;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }


    public void AddByVideoBarrage(Guid userGuid, string? videoBarrageBody)
    {
        VideoBarrageList!.Add(VideoBarrage.CreateVideoBarrage(VideoGuid, userGuid, videoBarrageBody));
    }

    public void AddByVideoReview(Guid userGuid, string? videoReviewBody)
    {
        VideoReviews!.Add(VideoReview.CreateVideoReview(VideoGuid, userGuid, videoReviewBody));
    }

    private void AddVideoTags(List<string> videoTags)
    {
        VideoTags.AddRange(videoTags);
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    public void SetProtectedEnd(DateTimeOffset timeOffset)
    {
        if (VideoControl.VideoProtectedTime!.StartTime >= timeOffset)
            throw new ArgumentException("结束时间不能小于开始时间");
        VideoControl.VideoProtectedTime!.SetEndTime(timeOffset);
    }
}