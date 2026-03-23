using Markdown.Domain.SeedWork;

namespace Markdown.Domain.Entities;

public class MarkReview : Entity
{
    private MarkReview()
    {
        MarkReviewGuid = Guid.CreateVersion7();
        MarkReviewTime = DateTime.UtcNow;
        MarkReviewAuth = MarkReviewAuth.ReviewAuthPublic;
        MarkReviews = new List<MarkReview>();
        MarkQuote = new MarkQuote();
    }


    public MarkReview(Guid markDownGuid, Guid userGuid, string markReviewContent) : this()
    {
        MarkDownGuid = markDownGuid;
        UserId = userGuid;
        MarkReviewContent = markReviewContent;
        MarkReviewTime = DateTime.Now;
        MarkAggregateRootGuid = Guid.Empty;
    }


    public Guid MarkReviewGuid { get; init; }

    public Guid MarkDownGuid { get; init; }

    public Guid UserId { get; init; }

    public Guid? MarkAggregateRootGuid { get; private set; }

    public string MarkReviewContent { get; private set; } = null!;

    public DateTime MarkReviewTime { get; private set; }

    public MarkReviewAuth MarkReviewAuth { get; private set; }

    public MarkDown MarkDown { get; private set; }

    public ICollection<MarkReview> MarkReviews { get; private set; }

    public MarkQuote MarkQuote { get; private set; }

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
}