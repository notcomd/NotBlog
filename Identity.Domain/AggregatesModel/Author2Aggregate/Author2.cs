namespace Identity.Domain.AggregatesModel.Author2Aggregate;

public class Author2 : Entity, IAggregateRoot
{
    public Guid AuthorId { get; init; }

    public Guid UserId { get; private set; }

    public string LoginProvider { get; private set; }=null!;

    public string ProviderKey { get; private set; }=null!;

    public string ProviderDisplayName { get; private set; }

    public string? AccessToken { get; private set; }

    public DateTimeOffset? AccessTokenExpiration { get; private set; }

    public string? RefreshToken { get; private set; }

    public DateTimeOffset CreatedDate { get; private set; }

    public DateTimeOffset? LastUsedAt { get; private set; }

    protected Author2()
    {
        AuthorId = Guid.CreateVersion7();
        CreatedDate=DateTimeOffset.UtcNow;
        LastUsedAt=DateTimeOffset.UtcNow;
    }

    public Author2(Guid authorId, string loginProvider, 
        string providerKey, string providerDisplayName):this()
    {
        AuthorId=authorId;
        LoginProvider=loginProvider;
        ProviderKey=providerKey;
        ProviderDisplayName=providerDisplayName;
    }
    
    public static Author2 Create(Guid authorId, string loginProvider, 
        string providerKey, string providerDisplayName)
    {
        return new Author2(authorId, loginProvider, providerKey, providerDisplayName);
    }

    public void UpdateTokens(string? accessToken, string? refreshToken, 
        DateTimeOffset? accessTokenExpiration)
    {
        AccessToken=accessToken;
        RefreshToken=refreshToken;
        AccessTokenExpiration=accessTokenExpiration;
        LastUsedAt=DateTimeOffset.UtcNow;
    }
    
    public void LinkUser(Guid userId)
    {
        UserId=userId;
    }
    
    public void UnlinkUser()
    {
        UserId=Guid.Empty;
    }
    
    

}