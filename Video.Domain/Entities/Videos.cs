
namespace Video.Domain.Entities;

/// <summary>
/// 视频实体
/// </summary>
public class Videos : Entity<Guid>, IAggregateRoot
{
    public Guid VideoGuid { get; init; }

    public List<Guid> Affiliated { get; private set; }

    public Uri VideoCover { get; private set; } = null!;

    public string VideoName { get; private set; } = null!;

    public string BriefIntroduction { get; private set; } = null!;

    public List<string> VideoTags { get; private set; }

    public Uri VideoFileUri { get; private set; } = null!;

    public string VideoNvid { get; init; } = null!;

    public bool IsDeleted { get; private set; }

    /// <summary>内容审核状态（默认草稿）。</summary>
    public VideoStatus Status { get; private set; }

    /// <summary>审核驳回原因（仅在 <see cref="Status"/> == Rejected 时非空，长度上限 500）。</summary>
    public string? RejectReason { get; private set; }

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
        Status = VideoStatus.Draft;
        TimeSpace = new TimeSpace(DateTime.UtcNow, DateTime.UtcNow);
    }

    public Videos(List<Guid> affiliatedAuthorizes, string videoName, Uri videoCover
        , Uri videoFileUri, string briefIntroduction, List<string> videoTags) : this()
    {
        AddByUser(affiliatedAuthorizes);
        VideoName = videoName;
        VideoCover = videoCover;
        VideoFileUri = videoFileUri;
        BriefIntroduction = briefIntroduction;
        AddVideoTags(videoTags);
        AddDomainEvent(new UploadVideoDomainEvent(VideoGuid, videoName, videoFileUri.ToString()));
    }


    private void AddByUser(List<Guid> affiliated)
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
    /// <param name="reviewGuid">评论 Guid（由调用方生成，便于追踪/通知）</param>
    /// <param name="userGuid">用户Guid</param>
    /// <param name="rootReview">是否为回复</param>
    /// <param name="videoReviewBody">评论内容</param>
    /// <param name="videoImage">图片</param>
    public void AddByVideoReview(Guid reviewGuid, Guid userGuid, Guid? rootReview, string? videoReviewBody, List<VideoImage>? videoImage)
    {
        VideoReviews!.Add(new VideoReview(reviewGuid, VideoGuid, userGuid, rootReview, videoReviewBody,
            videoImage is { Count: < 9 and > 0 } ? videoImage : null));
    }

    /// <summary>
    /// 添加视频标签
    /// </summary>
    /// <param name="videoTags">视频标签</param>
    private void AddVideoTags(List<string> videoTags)
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

    /// <summary>提交审核：草稿或被驳回的视频可提交，进入待审核状态，并清除旧驳回原因。</summary>
    /// <exception cref="InvalidOperationException">当前状态不是草稿或被驳回时抛出。</exception>
    public void SubmitForReview()
    {
        if (Status is not (VideoStatus.Draft or VideoStatus.Rejected))
            throw new InvalidOperationException($"当前视频状态为 {Status}，仅草稿或被驳回的视频可提交审核");
        Status = VideoStatus.Pending;
        RejectReason = null;
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 审核通过：仅待审核的视频可通过，置为已通过并公开展示。
    /// <para>维护不变量：<see cref="Status"/> == Approved ⟺ <c>VideoControl.VideoDisplay</c> == true。</para>
    /// </summary>
    /// <exception cref="InvalidOperationException">当前状态不是待审核时抛出。</exception>
    public void Approve()
    {
        if (Status != VideoStatus.Pending)
            throw new InvalidOperationException($"当前视频状态为 {Status}，仅待审核的视频可通过审核");
        Status = VideoStatus.Approved;
        VideoControl.Push();
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 审核驳回：仅待审核的视频可驳回，置为已拒绝、记录驳回原因并取消公开展示。
    /// <para>维护不变量：<see cref="Status"/> == Approved ⟺ <c>VideoControl.VideoDisplay</c> == true。</para>
    /// </summary>
    /// <param name="reason">驳回原因（不可为空，长度上限 500）。</param>
    /// <exception cref="InvalidOperationException">当前状态不是待审核时抛出。</exception>
    /// <exception cref="ArgumentException">驳回原因为空或超长时抛出。</exception>
    public void Reject(string reason)
    {
        if (Status != VideoStatus.Pending)
            throw new InvalidOperationException($"当前视频状态为 {Status}，仅待审核的视频可驳回");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("驳回原因不能为空", nameof(reason));
        if (reason.Length > 500)
            throw new ArgumentException("驳回原因不能超过 500 个字符", nameof(reason));
        Status = VideoStatus.Rejected;
        RejectReason = reason;
        VideoControl.Withdraw();
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

    /// <summary>编辑门禁：草稿与被驳回的视频可编辑；待审核与已通过的视频不可编辑。</summary>
    /// <exception cref="InvalidOperationException">当前状态为待审核或已通过时抛出。</exception>
    public void EnsureEditable()
    {
        if (Status is VideoStatus.Pending or VideoStatus.Approved)
            throw new InvalidOperationException($"当前视频状态为 {Status}，不允许编辑内容");
    }

    /// <summary>更新视频内容（受 <see cref="EnsureEditable"/> 门禁约束）。</summary>
    /// <param name="videoName">视频名称</param>
    /// <param name="videoCover">封面 Uri</param>
    /// <param name="videoFileUri">视频文件 Uri</param>
    /// <param name="briefIntroduction">简介</param>
    /// <param name="videoTags">标签</param>
    /// <exception cref="InvalidOperationException">当前状态不允许编辑时抛出。</exception>
    public void UpdateContent(string videoName, Uri videoCover, Uri videoFileUri, string briefIntroduction,
        List<string> videoTags)
    {
        EnsureEditable();
        VideoName = videoName ?? throw new ArgumentNullException(nameof(videoName));
        VideoCover = videoCover ?? throw new ArgumentNullException(nameof(videoCover));
        VideoFileUri = videoFileUri ?? throw new ArgumentNullException(nameof(videoFileUri));
        BriefIntroduction = briefIntroduction ?? throw new ArgumentNullException(nameof(briefIntroduction));
        VideoTags = videoTags ?? throw new ArgumentNullException(nameof(videoTags));
        TimeSpace.ResetUpdateAt(DateTimeOffset.UtcNow);
    }

}