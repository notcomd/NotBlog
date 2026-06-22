namespace Identity.Domain.Options;

public class GitHubOptions
{
    /// <summary>
    /// 
    /// </summary>
    public string AuthorUrl { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public bool Enable { get; set; } = false;

    /// <summary>
    /// 
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// </summary>
    public HashSet<string> Scope { get; set; } = new() { };

    /// <summary>
    /// 密钥
    /// </summary>
    public string PrivateKey { get; set; } = string.Empty;
}