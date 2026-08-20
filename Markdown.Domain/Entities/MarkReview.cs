namespace Markdown.Domain.Entities;

public class MarkReview : Entity<int>
{
    private MarkReview()
    {
        MarkReviewGuid = Guid.CreateVersion7();
        MarkReviewTime = DateTimeOffset.UtcNow;
        MarkReviewAuth = MarkReviewAuth.ReviewAuthPublic;
        MarkReviews = new List<MarkReview>();
        ReviewImages = new List<ReviewImage>();
        ReviewQuote = new ReviewQuote();
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

    public MarkDown MarkDown { get; private set; } = null!;

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public ICollection<ReviewImage>? ReviewImages { get; private set; }

    /// <summary>
    ///     评论交互统计（点赞/查看/回复数/踩，值对象）
    /// </summary>
    public ReviewQuote ReviewQuote { get; private set; }

    public bool IsDelete { get; private set; }

    public Task<MarkReview> UpdateByMarkReviewAuthAsync(MarkReviewAuth markReviewAuth)
    {
        MarkReviewAuth = markReviewAuth;
        return Task.FromResult(this);
    }

    /// <summary>
    ///     更新评论内容
    /// </summary>
    public void UpdateContent(string content)
    {
        MarkReviewContent = content ?? throw new ArgumentNullException(nameof(content));
    }

    public void AddReviewImage(ReviewImage reviewImage)
    {
        ReviewImages?.Add(reviewImage);
    }

    public void SoftDelete() => IsDelete = true;
}
