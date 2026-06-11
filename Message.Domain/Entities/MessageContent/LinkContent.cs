using Message.Domain.SeedWork;

namespace Message.Domain.Entities.MessageContent;

public class LinkContent : ValueObject
{
    private LinkContent(Uri url, string? title = null, string? description = null)
    {
        Url = url ?? throw new ArgumentNullException(nameof(url));
        Title = title;
        Description = description;
    }

    public Uri Url { get; }
    public string? Title { get; }
    public string? Description { get; }

    public static LinkContent Create(Uri url, string? title = null, string? description = null)
        => new(url, title, description);

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Url;
        yield return Title ?? string.Empty;
        yield return Description ?? string.Empty;
    }
}