using Commons.SeedWork;
using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

/// <summary>
/// 视频评论实体 — 支持文本、图片、视频、富文本四种内容类型。
///
/// 设计要点：
/// - ReviewContent 值对象封装内容类型判别和验证逻辑
/// - 保留 VideoReviewBody / VideoImages 属性用于向后兼容
/// - ContentType 字符串字段用于数据库持久化和快速查询
/// - 工厂方法覆盖所有内容类型，扩展新类型只需修改 ReviewContent
/// </summary>
public class VideoReview : Entity<int>
{
    public Guid VideoReviewGuid { get; init; }

    public Guid VideoGuid { get; init; }

    public Guid UserGuid { get; init; }

    public Guid? RootReview { get; private set; }

    /// <summary>内容类型判别字段 (text/image/video/richtext)</summary>
    public string ContentType => Content.ContentType;

    /// <summary>
    /// 评论内容值对象 — 封装了所有内容类型的存储和验证。
    /// EF Core 通过 OwnsOne() 映射到数据库。
    /// </summary>
    public ReviewContent Content { get; private set; }

    /// <summary>
    /// [向后兼容] 评论正文文本。
    /// 代理到 Content.Body，保留此属性使现有代码无需修改。
    /// </summary>
    public string? VideoReviewBody => Content.Body;

    /// <summary>
    /// [向后兼容] 评论内嵌图片/视频列表。
    /// 代理到 Content.MediaItems，保留此属性使现有代码无需修改。
    /// </summary>
    public ICollection<VideoImage> VideoImages
    {
        get => Content.MediaItems;
        private set
        {
            Content.MediaItems.Clear();
            if (value is not null)
            {
                foreach (var img in value) Content.MediaItems.Add(img);
            }
        }
    }

    public TimeSpace TimeSpace { get; private set; }

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; }

    public ICollection<VideoReview>? VideoReviews { get; private set; }

    // ── 构造器 ──

    private VideoReview()
    {
        VideoReviewGuid = Guid.CreateVersion7();
        TimeSpace = new TimeSpace(DateTime.UtcNow, DateTime.UtcNow);
        VideoControl = VideoControl.VideoControlBuilder();
        VideoQuote = VideoQuote.VideoQuoteBuilder();
        Content = ReviewContent.CreateText(string.Empty);
        VideoReviews = [];
    }

    /// <summary>
    /// [向后兼容] 传统构造器：文本 + 图片混合评论。
    /// 自动推断内容类型：纯文本 → Text，纯图片 → Image，混合 → RichText。
    /// </summary>
    public VideoReview(Guid videoGuid, Guid userGuid, Guid? rootGuid,
        string? videoReviewBody, List<VideoImage>? videoImages) : this()
    {
        VideoGuid = videoGuid;
        UserGuid = userGuid;
        RootReview = rootGuid;
        Content = ReviewContent.CreateDefault(videoReviewBody, videoImages);
    }

    /// <summary>
    /// 创建纯文本评论。
    /// </summary>
    public static VideoReview CreateTextReview(Guid videoGuid, Guid userGuid,
        string body, Guid? rootGuid = null)
    {
        var review = new VideoReview
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            RootReview = rootGuid
        };
        review.Content = ReviewContent.CreateText(body);
        return review;
    }

    /// <summary>
    /// 创建图片评论。
    /// </summary>
    public static VideoReview CreateImageReview(Guid videoGuid, Guid userGuid,
        List<VideoImage> images, string? caption = null, Guid? rootGuid = null)
    {
        var review = new VideoReview
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            RootReview = rootGuid
        };
        review.Content = ReviewContent.CreateImage(images, caption);
        return review;
    }

    /// <summary>
    /// 创建视频评论。
    /// </summary>
    public static VideoReview CreateVideoReview(Guid videoGuid, Guid userGuid,
        List<VideoImage> videoItems, string? description = null, Guid? rootGuid = null)
    {
        var review = new VideoReview
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            RootReview = rootGuid
        };
        review.Content = ReviewContent.CreateVideo(videoItems, description);
        return review;
    }

    /// <summary>
    /// 创建富文本评论。
    /// </summary>
    public static VideoReview CreateRichTextReview(Guid videoGuid, Guid userGuid,
        string richTextBody, Guid? rootGuid = null)
    {
        var review = new VideoReview
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            RootReview = rootGuid
        };
        review.Content = ReviewContent.CreateRichText(richTextBody);
        return review;
    }

    // ── 行为方法 ──

    public void AddByRootReview(Guid rootGuid)
    {
        RootReview = rootGuid;
    }

    /// <summary>更新内容为纯文本</summary>
    public void UpdateContentToText(string body)
    {
        Content = ReviewContent.CreateText(body);
        TimeSpace.ResetUpdateAt(DateTime.UtcNow);
    }

    /// <summary>更新内容为图片</summary>
    public void UpdateContentToImage(List<VideoImage> images, string? caption = null)
    {
        Content = ReviewContent.CreateImage(images, caption);
        TimeSpace.ResetUpdateAt(DateTime.UtcNow);
    }

    /// <summary>更新内容为视频</summary>
    public void UpdateContentToVideo(List<VideoImage> videoItems, string? description = null)
    {
        Content = ReviewContent.CreateVideo(videoItems, description);
        TimeSpace.ResetUpdateAt(DateTime.UtcNow);
    }

    /// <summary>更新内容为富文本</summary>
    public void UpdateContentToRichText(string richTextBody)
    {
        Content = ReviewContent.CreateRichText(richTextBody);
        TimeSpace.ResetUpdateAt(DateTime.UtcNow);
    }

    /// <summary>
    /// 完整数据验证（包含内容类型特定规则）。
    /// </summary>
    public (bool IsValid, string? Error) Validate()
    {
        if (VideoGuid == Guid.Empty)
            return (false, "VideoGuid is required.");
        if (UserGuid == Guid.Empty)
            return (false, "UserGuid is required.");
        if (!ReviewContentType.IsValid(ContentType))
            return (false, $"Unknown content type: '{ContentType}'.");

        return (true, null);
    }

    // ── Builder ──

    public class VideoReviewBuilder
    {
        private Guid _videoGuid;
        private Guid _userGuid;
        private Guid? _rootGuid;
        private string? _contentType;           // 新增
        private string? _videoReviewBody;
        private List<VideoImage>? _videoImages;

        public VideoReviewBuilder WithVideoGuid(Guid videoGuid)
        {
            _videoGuid = videoGuid;
            return this;
        }

        public VideoReviewBuilder WithUserGuid(Guid userGuid)
        {
            _userGuid = userGuid;
            return this;
        }

        public VideoReviewBuilder WithRootGuid(Guid? rootGuid)
        {
            _rootGuid = rootGuid;
            return this;
        }

        /// <summary>
        /// [新增] 指定内容类型。设置后将使用对应工厂方法创建。
        /// 若不设置则使用 CreateDefault 自动推断。
        /// </summary>
        public VideoReviewBuilder WithContentType(string contentType)
        {
            _contentType = contentType;
            return this;
        }

        public VideoReviewBuilder WithVideoReviewBody(string? videoReviewBody)
        {
            _videoReviewBody = videoReviewBody;
            return this;
        }

        public VideoReviewBuilder WithVideoImages(List<VideoImage>? videoImages)
        {
            _videoImages = videoImages;
            return this;
        }

        public VideoReview Build()
        {
            // 如果指定了内容类型，使用对应工厂方法
            if (_contentType is not null)
            {
                return _contentType.ToLowerInvariant() switch
                {
                    ReviewContentType.Text => CreateTextReview(
                        _videoGuid, _userGuid, _videoReviewBody ?? string.Empty, _rootGuid),
                    ReviewContentType.Image => CreateImageReview(
                        _videoGuid, _userGuid, _videoImages ?? [],
                        _videoReviewBody, _rootGuid),
                    ReviewContentType.Video => CreateVideoReview(
                        _videoGuid, _userGuid, _videoImages ?? [],
                        _videoReviewBody, _rootGuid),
                    ReviewContentType.RichText => CreateRichTextReview(
                        _videoGuid, _userGuid, _videoReviewBody ?? string.Empty, _rootGuid),
                    _ => new VideoReview(_videoGuid, _userGuid, _rootGuid,
                        _videoReviewBody, _videoImages)
                };
            }

            // 向后兼容：使用默认推断
            return new VideoReview(_videoGuid, _userGuid, _rootGuid,
                _videoReviewBody, _videoImages);
        }
    }
}
