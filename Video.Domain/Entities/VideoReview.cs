using Video.Domain.ValueObjects;

namespace Video.Domain.Entities;

public record VideoReview
{
    /// <summary>
    ///     主键
    /// </summary>
    public Guid VideoReviewGuid { get; init; } = Guid.NewGuid();

    /// <summary>
    ///     视频主键
    /// </summary>
    public required Guid VideoGuid { get; init; }

    /// <summary>
    ///     用户主键
    /// </summary>
    public required Guid UserGuid { get; init; }

    /// <summary>
    ///     根评论的主键
    /// </summary>
    public Guid? RootReview { get; private set; }

    /// <summary>
    ///     评论内容
    /// </summary>
    public string? VideoReviewBody { get; init; }

    public TimeSpace TimeSpace { get; private set; } 

    public VideoControl VideoControl { get; private set; }

    public VideoQuote VideoQuote { get; private set; } = VideoQuote.VideoQuoteBuilder();


    public static VideoReview CreateVideoReview(Guid videoGuid, Guid userGuid, string? videoReviewBody)
    {
        return new VideoReview
        {
            VideoGuid = videoGuid,
            UserGuid = userGuid,
            VideoReviewBody = videoReviewBody
        };
    }

    public void AddByRootReview(Guid rootGuid)
    {
        RootReview = rootGuid;
    }
}