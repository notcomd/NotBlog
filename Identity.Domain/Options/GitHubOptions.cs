namespace Identity.Domain.Options;

public class GitHubOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enable { get; set; } = false;
}