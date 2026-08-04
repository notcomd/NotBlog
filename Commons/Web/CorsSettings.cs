namespace Commons.Web;

/// <summary>
/// CORS 配置选项
/// </summary>
public class CorsSettings
{
    /// <summary>允许的来源列表</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
