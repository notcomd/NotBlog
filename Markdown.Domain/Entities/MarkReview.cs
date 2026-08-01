namespace Markdown.Domain.Entities;

public class MarkReview : Entity
{
    private MarkReview()
    {
        MarkReviewGuid = Guid.CreateVersion7();
        MarkReviewTime = DateTimeOffset.UtcNow;
        MarkReviewAuth = MarkReviewAuth.ReviewAuthPublic;
        MarkReviews = new List<MarkReview>();
        ReviewImages = new List<ReviewImage>();
        MarkQuote = new MarkQuote();
    }


    public MarkReview(Guid markDownGuid, Guid userGuid, string? markReviewContent,
        List<ReviewImage>? reviewImage) : this()
    {
        MarkDownGuid = markDownGuid;
        UserId = userGuid;
        MarkReviewContent = markReviewContent;
        ReviewImages = reviewImage;
        MarkReviewTime = DateTimeOffset.UtcNow;
        //MarkAggregateRootGuid = Guid.Empty;
    }


    public Guid MarkReviewGuid { get; init; }

    public Guid MarkDownGuid { get; init; }

    public Guid UserId { get; init; }

    public Guid? MarkAggregateRootGuid { get; private set; }

    /// <summary>
    ///     由聚合根调用，设置父评论 GUID
    /// </summary>
    internal void SetParentReviewGuid(Guid parentReviewGuid) => MarkAggregateRootGuid = parentReviewGuid;

    public string? MarkReviewContent { get; private set; } = null!;

    public DateTimeOffset MarkReviewTime { get; private set; }

    public MarkReviewAuth MarkReviewAuth { get; private set; }

    public MarkDown MarkDown { get; private set; }

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public ICollection<ReviewImage>? ReviewImages { get; private set; }

    public MarkQuote MarkQuote { get; private set; }

    public bool IsDelete { get; private set; }

    public Task<MarkReview> AddToChildReviewAsync(Guid aggregateRootGuid, MarkReview markReview)
    {
        markReview.MarkAggregateRootGuid = aggregateRootGuid;
        MarkReviews.Add(markReview);
        return Task.FromResult(markReview);
    }

    public Task<MarkReview> UpDataByMarkReviewAuthAsync(MarkReviewAuth markReviewAuth)
    {
        MarkReviewAuth = markReviewAuth;
        return Task.FromResult(this);
    }

    /// <summary>
    ///     更新评论内容
    /// </summary>
    /// <param name="content">新的评论内容</param>
    public void UpdateContent(string content)
    {
        MarkReviewContent = content ?? throw new ArgumentNullException(nameof(content));
    }

    public void AddReviewImage(ReviewImage reviewImage)
    {
        ReviewImages?.Add(reviewImage);
    }

    public void SoftDelete() => IsDelete = true;

    public void ResetDelete() => IsDelete = false;
}