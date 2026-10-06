namespace Identity.Domain.Options;

public class OAuthOptions
{
    /// <summary>
    /// 模板占位符前缀：凡以此开头的凭据一律视为「未配置」，避免把模板默认值当作真实密钥。
    /// </summary>
    private static readonly string[] PlaceholderPrefixes = ["your-", "your_"];

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

    public QQOptions QQOptions { get; set; } = new();

    /// <summary>
    /// 判定指定提供商是否对外呈现为「可用」：<c>Enabled</c> 为真且关键凭据齐备。
    /// 各提供商的凭据字段不同：Google/GitHub/Microsoft 取 ClientId + ClientSecret，
    /// WeChat 取 AppId + AppSecret，QQ 取 AppId + AppKey。
    /// 统一在此实现，避免在各调用处散落判断。
    /// </summary>
    /// <param name="provider">提供商键（google / github / microsoft / wechat / qq，大小写不敏感）</param>
    /// <returns>可用返回 true；未知提供商或凭据不齐备返回 false</returns>
    public bool IsProviderAvailable(string provider) => provider?.ToLowerInvariant() switch
    {
        "google" => GoogleOptions.Enabled && HasCredentials(GoogleOptions.ClientId, GoogleOptions.ClientSecret),
        "github" => GitHubOptions.Enable && HasCredentials(GitHubOptions.ClientId, GitHubOptions.ClientSecret),
        "microsoft" => MicrosoftOptions.Enable && HasCredentials(MicrosoftOptions.ClientId, MicrosoftOptions.ClientSecret),
        "wechat" => WeChatOptions.Enabled && HasCredentials(WeChatOptions.AppId, WeChatOptions.AppSecret),
        "qq" => QQOptions.Enabled && HasCredentials(QQOptions.AppId, QQOptions.AppKey),
        _ => false
    };

    /// <summary>
    /// 判定一组凭据是否齐备：全部非空白，且均不是模板占位符（如以 your-/your_ 开头）。
    /// </summary>
    /// <param name="values">待校验的凭据值</param>
    /// <returns>齐备返回 true</returns>
    private static bool HasCredentials(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            if (PlaceholderPrefixes.Any(prefix =>
                    trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}