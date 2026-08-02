using System.ComponentModel.DataAnnotations;

namespace NotBlog_Yarp.Permission;

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
    /// 权限服务失败时的降级策略（F-12）：
    /// "Open"（默认）= fail-open：权限服务异常/不可用/端点缺失时放行请求并记录日志（可用性优先）；
    /// "Closed" = fail-closed：权限服务异常时拒绝（403，安全性优先）。
    /// </summary>
    public string FailPolicy { get; init; } = "Open";

    /// <summary>是否 fail-open（权限服务失败时放行）</summary>
    public bool FailOpen => !string.Equals(FailPolicy, "Closed", StringComparison.OrdinalIgnoreCase);

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

    public sealed class DevUser
    {
        /// <summary>该用户拥有的所有权限编码</summary>
        public HashSet<string> Permissions { get; init; } = new();

        /// <summary>数据范围（格式: "type|value1,value2,..."）</summary>
        public string DataScope { get; init; } = "0|";
    }
}
