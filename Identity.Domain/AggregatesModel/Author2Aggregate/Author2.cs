namespace Identity.Domain.AggregatesModel.Author2Aggregate;

public class Author2 : Entity, IAggregateRoot
{


    protected Author2()
    { 
        Id = Guid.CreateVersion7(); 
    }



    public static Task<Author2> CreateByAuthor2Async(Guid userGuid, string authorName, string authorDescription, string authorPrivateKey, string authorSecret)
    {

        ArgumentNullException.ThrowIfNull(authorName);
        ArgumentNullException.ThrowIfNull(authorDescription);
        ArgumentNullException.ThrowIfNull(authorPrivateKey);
        ArgumentNullException.ThrowIfNull(authorSecret);

        var author = new Author2
        {
            Id = Guid.CreateVersion7(),
            UserGuid = userGuid != Guid.Empty ? userGuid : throw new ArgumentNullException(nameof(userGuid), "UserGuid cannot be empty"),
            AuthorName = authorName ?? throw new ArgumentNullException(nameof(authorName)),
            AuthorDescription = authorDescription ?? throw new ArgumentNullException(nameof(authorDescription)),
            AuthorPrivateKey = authorPrivateKey ?? throw new ArgumentNullException(nameof(authorPrivateKey)),
            AuthorSecret = authorSecret ?? throw new ArgumentNullException(nameof(authorSecret))
        };
        return Task.FromResult(author);
    }

   

    public Guid UserGuid { get; private set; }

    public string AuthorName { get; private set; } = string.Empty;

    public string AuthorDescription { get; private set; } = string.Empty;

    public string AuthorPrivateKey { get; private set; } = string.Empty;

    public string AuthorSecret { get; private set; } = string.Empty;




    public void SetAuthorName(string authorName)
    {
        ArgumentNullException.ThrowIfNull(authorName);
        AuthorName = authorName;
    }

    public void SetAuthorDescription(string authorDescription)
    {
        ArgumentNullException.ThrowIfNull(authorDescription);
        AuthorDescription = authorDescription;
    }

    public void SetAuthorPrivateKey(string authorPrivateKey)
    {
        ArgumentNullException.ThrowIfNull(authorPrivateKey);
        AuthorPrivateKey = authorPrivateKey;
    }

    public void SetAuthorSecret(string authorSecret)
    {
        ArgumentNullException.ThrowIfNull(authorSecret);
        AuthorSecret = authorSecret;
    }
}