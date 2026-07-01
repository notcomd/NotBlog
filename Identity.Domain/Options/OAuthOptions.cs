namespace Identity.Domain.Options;

public class OAuthOptions
{
    public GoogleOptions GoogleOptions { get; set; } = new();

    public GitHubOptions GitHubOptions { get; set; } = new();

    public MicrosoftOptions MicrosoftOptions { get; set; } = new();

    public WeChatOptions WeChatOptions { get; set; } = new();
}