namespace Markdown.Domain.Entities;

public class MarkQuote
{
    public Guid MarkQuoteGuid { get; init; } = Guid.NewGuid();

    public Guid MarkQRGuid { get; private set; }

    public long LoveSome { get; private set; }

    public long ReviewSome { get; private set; }

    public long CommentSome { get; private set; }

    public MarkDown Markdown { get; private set; }

    public MarkReview MarkReview { get; private set; }
}