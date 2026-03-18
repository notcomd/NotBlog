namespace Identity.Web.API.Options;

public class OAuthOptions
{
    public GoogleOptions Google { get; set; }=new();
    public GitHubOptions Github { get; set; }=new();
    public MicrosoftOptions Microsoft { get; set; }=new();
}