namespace Identity.Domain.Options;

public class OAuthOptions
{
    public GoogleOptions Google { get; set; } = new();
    public GitHubOptions GitHub { get; set; } = new();
    public MicrosoftOptions Microsoft { get; set; } = new();
    public WeChatOptions WeChat { get; set; } = new();
}