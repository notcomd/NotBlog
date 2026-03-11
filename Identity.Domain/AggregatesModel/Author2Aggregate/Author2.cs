namespace Identity.Domain.AggregatesModel.Author2Aggregate;

public class Author2 : Entity, IAggregateRoot
{
    protected Author2()
    {
    }


    protected Author2(string authorName, string authorDescription, string authorPrivateKey, string authorSecret)
    {
        AuthorName = authorName ?? throw new ArgumentNullException(nameof(authorName));
        AuthorDescription = authorDescription ?? throw new ArgumentNullException(nameof(authorDescription));
        AuthorPrivateKey = authorPrivateKey ?? throw new ArgumentNullException(nameof(authorPrivateKey));
        AuthorSecret = authorSecret ?? throw new ArgumentNullException(nameof(authorSecret));
    }

    public string AuthorName { get; private set; } = string.Empty;

    public string AuthorDescription { get; private set; } = string.Empty;

    public string AuthorPrivateKey { get; private set; } = string.Empty;

    public string AuthorSecret { get; private set; } = string.Empty;
}