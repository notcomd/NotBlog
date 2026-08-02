namespace Identity.Domain.Options;

public class OAuthOptions
{
    /// <summary>
    /// 允许的回调重定向 URI 白名单（S-11）。
    /// 支持精确匹配，或以 "*" 结尾的前缀匹配（如 "https://localhost:9091/*"）。
    /// 为空时不做校验（向后兼容，但生产环境应配置）。
    /// </summary>
    public string[] AllowedRedirectUris { get; set; } = Array.Empty<string>();

    public GoogleOptions GoogleOptions { get; set; } = new();

    public GitHubOptions GitHubOptions { get; set; } = new();

    public MicrosoftOptions MicrosoftOptions { get; set; } = new();

    public WeChatOptions WeChatOptions { get; set; } = new();
}