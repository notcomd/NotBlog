namespace Markdown.Domain.Entities;

public class MarkdownAuthorize
{
    
    public Guid  AuthorizeGuid { get; init; }
    public string  UserAuthorize { get; private set; }
    private Authorize  A_markdonw { get; set; }
    
    private MarkdownAuthorize(){}

    
    public MarkdownAuthorize(ref string userAuthorize,ref Authorize authorize)
    {
        this.AuthorizeGuid = new Guid();
        this.A_markdonw = authorize;
        this.UserAuthorize = userAuthorize;
    }
    
    public Task<MarkdownAuthorize> UpAuthorize(ref Authorize authorize)
    {
        this.A_markdonw = authorize;
        return Task.FromResult(this);
    }
    
}