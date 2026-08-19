using System.ComponentModel.DataAnnotations;

namespace NotBlog_Yarp.Options;

/// <summary>
/// 权限路由配置选项
///
/// 从 appsettings.json 的 "PermissionRoutes" 节绑定。
/// 使用 IOptions<T> 模式，支持配置校验和 IOptionsSnapshot 热重载。
/// </summary>
public class PermissionOptions
{
    public const string SectionName = "PermissionRoutes";

    /// <summary>公开路径前缀列表（以这些前缀开头的路径无需鉴权，前缀匹配）</summary>
    public string[] PublicPaths { get; init; } = [];

    /// <summary>精确绕过路径列表（在已映射的权限前缀下，对特定精确路径放行）</summary>
    public string[] BypassPaths { get; init; } = [];

    /// <summary>HTTP 方法 → 路径模式 → 权限编码的映射列表（可为空数组，未映射的路径默认放行）</summary>
    public RouteMapping[] Mappings { get; init; } = [];

    /// <summary>
    /// 权限服务失败时的降级策略（F-12 / V5）：
    /// "Closed"（默认）= fail-closed：权限服务异常/不可用/返回 4xx 时拒绝（403，安全性优先）；
    /// "Open" = fail-open：仅 5xx/429（服务不可用）时放行并记录日志（可用性优先）。
    /// 注意：V5 起 4xx（404 端点缺失/401 凭证错误/400 参数错误）一律 fail-closed，
    /// 不再参与降级 —— 配置或调用错误若放行会使权限体系形同虚设。
    /// </summary>
    [RegularExpression("^(?i)(Open|Closed)$", ErrorMessage = "FailPolicy 仅支持 Open 或 Closed")]
    public string FailPolicy { get; init; } = "Closed";

    /// <summary>是否 fail-open（权限服务失败时放行）</summary>
    public bool FailOpen => !string.Equals(FailPolicy, "Closed", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 未映射路径的默认策略（P0-V3）：
    /// "Deny"（默认）= 未配置权限映射的路径一律拒绝（403，白名单模式，安全性优先）；
    /// "Allow" = 放行（兼容旧行为，仅建议开发环境使用）。
    /// </summary>
    public string DefaultPolicy { get; init; } = "Deny";

    /// <summary>未映射路径是否放行（DefaultPolicy == "Allow"）</summary>
    public bool DefaultAllow => string.Equals(DefaultPolicy, "Allow", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 权限映射后台刷新间隔（秒）。
    /// 默认 300 秒（5 分钟）。设为 0 则禁用后台轮询（仅启动时加载一次）。
    /// 仅在 IdentityService:BaseUrl 已配置（生产模式）时生效。
    /// </summary>
    public int RefreshIntervalSeconds { get; init; } = 300;

    /// <summary>是否启用后台刷新（RefreshIntervalSeconds > 0）</summary>
    public bool RefreshEnabled => RefreshIntervalSeconds > 0;

    /// <summary>开发用户配置（仅 ConfigPermissionServiceClient 使用）</summary>
    public Dictionary<string, DevUser>? DevUsers { get; init; }

    public sealed class RouteMapping
    {
        /// <summary>HTTP 方法（GET/POST/PUT/DELETE 等）</summary>
        [Required]
        public string Method { get; init; } = string.Empty;

        /// <summary>路径模式，支持 {param} 和 {**catch-all}</summary>
        [Required]
        public string Path { get; init; } = string.Empty;

        /// <summary>对应的权限编码（如 "api:article:read"）</summary>
        [Required]
        public string Code { get; init; } = string.Empty;
    }


}
