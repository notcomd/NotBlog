using Markdown.Domain.Entities;

namespace Markdown.Domain.DomainEvent;

public class CreateMarkReviewDomianEvent
{

    public Guid MarkReviewId { get; }

    public Guid UserGuid { get; }

    public MarkReviewType MarkReviewType { get; private set; }

    public string MarkReviewContent { get; } = null!;

    public DateTime MarkReviewTime { get; }

    public Guid? MarkAggregateRootGuid { get; }

    public DateTime CreateDateTime { get; }

    public CreateMarkReviewDomianEvent(Guid markReviewId, Guid userGuid, MarkReviewType markReviewType, string markReviewContent, DateTime markReviewTime, Guid? markAggregateRootGuid, DateTime createDateTime)
    {
        MarkReviewId = markReviewId;
        UserGuid = userGuid;
        MarkReviewType = markReviewType;
        MarkReviewContent = markReviewContent;
        MarkReviewTime = markReviewTime;
        MarkAggregateRootGuid = markAggregateRootGuid;
        CreateDateTime = createDateTime;
    }

}
