using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace NotBlog_Yarp.Permission;

/// <summary>
/// URL → PermissionCode 路由映射表
/// 
/// 维护网关需要鉴权的全部路由与权限码对应关系。
/// 配置来源：appsettings.json 的 "PermissionRoutes" 节。
/// 支持路径参数匹配（如 /api/articles/{id}）。
/// 
/// 线程安全：PermissionMappingInitializer 会在启动后从 Identity 拉取映射并
/// 并发替换（ClearMappings/AddMap），因此对 _entries 的读写统一加锁，
/// 正则缓存使用 ConcurrentDictionary，保证与请求处理并发安全。
/// </summary>
public class PermissionRouteMap
{
    private readonly object _lock = new();
    private readonly List<RouteEntry> _entries = new();
    private readonly ConcurrentDictionary<string, Regex> _regexCache = new();
    private readonly HashSet<string> _publicPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _bypassPaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>已注册的映射条目数</summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>
    /// 清空全部 URL→PermissionCode 映射条目（保留 PublicPaths 和 BypassPaths）
    /// 用于从 Identity 重新加载映射时替换旧数据
    /// </summary>
    public void ClearMappings()
    {
        lock (_lock)
        {
            _entries.Clear();
            _regexCache.Clear();
        }
    }

    /// <summary>
    /// 注册一条 URL→PermissionCode 映射
    /// </summary>
    /// <param name="method">HTTP 方法（GET/POST/PUT/DELETE 等）</param>
    /// <param name="pathPattern">路径模式，支持 {param} 参数占位</param>
    /// <param name="permissionCode">对应的权限编码</param>
    public void AddMap(string method, string pathPattern, string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathPattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        lock (_lock)
        {
            _entries.Add(new RouteEntry(method.ToUpperInvariant(), pathPattern, permissionCode));
        }
    }

    /// <summary>
    /// 注册公开路径（无需鉴权），前缀匹配
    /// </summary>
    /// <param name="pathPrefix">路径前缀，以该前缀开头的路径均公开</param>
    public void AddPublicPath(string pathPrefix)
    {
        if (!string.IsNullOrWhiteSpace(pathPrefix))
            _publicPaths.Add(pathPrefix.TrimEnd('/'));
    }

    /// <summary>
    /// 注册精确绕过路径 — 在已映射的权限前缀下，放行特定精确路径
    /// 例如: /api/identity/{**catch-all} 映射需要权限，但 /api/identity/login 应公开
    /// </summary>
    /// <param name="exactPath">精确路径（区分大小写忽略）</param>
    public void AddBypassPath(string exactPath)
    {
        if (!string.IsNullOrWhiteSpace(exactPath))
            _bypassPaths.Add(exactPath.TrimEnd('/'));
    }

    /// <summary>
    /// 判断路径是否为公开路径（无需鉴权）
    /// 先检查精确绕过路径，再检查前缀公开路径
    /// </summary>
    public bool IsPublicPath(string path)
    {
        var normalized = path.TrimEnd('/');

        // 1. 精确绕过路径
        if (_bypassPaths.Contains(normalized))
            return true;

        // 2. 前缀公开路径
        return _publicPaths.Any(p =>
            normalized.StartsWith(p, StringComparison.OrdinalIgnoreCase) &&
            (normalized.Length == p.Length || normalized[p.Length] == '/'));
    }

    /// <summary>
    /// 尝试匹配路径和方法，输出对应的权限编码
    /// </summary>
    /// <param name="path">请求路径</param>
    /// <param name="method">HTTP 方法</param>
    /// <param name="permissionCode">匹配到的权限编码（匹配失败时为 empty）</param>
    /// <returns>是否匹配成功</returns>
    public bool TryMatch(string path, string method, out string permissionCode)
    {
        // 在锁内快照条目，避免与 PermissionMappingInitializer 的并发替换冲突
        RouteEntry[] entries;
        lock (_lock)
        {
            entries = _entries.ToArray();
        }

        foreach (var entry in entries)
        {
            if (!string.Equals(entry.Method, method, StringComparison.OrdinalIgnoreCase))
                continue;

            if (MatchPattern(path, entry.PathPattern))
            {
                permissionCode = entry.PermissionCode;
                return true;
            }
        }

        permissionCode = string.Empty;
        return false;
    }

    /// <summary>
    /// 从 IConfiguration 加载配置（兼容旧用法）
    /// </summary>
    public void LoadFromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("PermissionRoutes");

        // 公开路径
        var publicPaths = section.GetSection("PublicPaths").Get<string[]>();
        if (publicPaths is not null)
        {
            foreach (var p in publicPaths)
                AddPublicPath(p);
        }

        // 精确绕过路径
        var bypassPaths = section.GetSection("BypassPaths").Get<string[]>();
        if (bypassPaths is not null)
        {
            foreach (var p in bypassPaths)
                AddBypassPath(p);
        }

        // 权限映射
        var mappings = section.GetSection("Mappings").Get<List<RouteMappingConfig>>();
        if (mappings is not null)
        {
            foreach (var m in mappings)
                AddMap(m.Method, m.Path, m.Code);
        }
    }

    /// <summary>
    /// 从 PermissionOptions 加载配置（推荐用法，支持 IOptions 校验和热重载）
    /// </summary>
    public static PermissionRouteMap FromOptions(PermissionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var map = new PermissionRouteMap();

        foreach (var p in options.PublicPaths)
            map.AddPublicPath(p);

        foreach (var p in options.BypassPaths)
            map.AddBypassPath(p);

        foreach (var m in options.Mappings)
            map.AddMap(m.Method, m.Path, m.Code);

        return map;
    }

    /// <summary>
    /// 将路径模式中的 {param} / {**param} 替换为正则进行匹配
    ///   /{**param}    → (?:/.*)?  （多段，可匹配空剩余，含尾斜杠）
    ///   {**param}     → .*        （多段）
    ///   {param}       → [^/]+     （单段）
    /// </summary>
    private bool MatchPattern(string path, string pattern)
    {
        var regex = _regexCache.GetOrAdd(pattern, p =>
        {
            // 1. 先处理 ** 通配符（多段）
            var regexPattern = Regex.Escape(p)
                .Replace("\\{", "{")
                .Replace("\\}", "}");

            // 带斜杠的 catch-all（如 /{**catch-all}）→ 可选路径，允许空剩余
            regexPattern = Regex.Replace(regexPattern,
                @"/\{\*\*[^}]+\}", "(?:/.*)?");
            // 剩余 catch-all（不带前导斜杠）→ 任意剩余
            regexPattern = Regex.Replace(regexPattern,
                @"\{\*\*[^}]+\}", ".*");
            // 2. 再处理普通参数（单段）
            regexPattern = Regex.Replace(regexPattern,
                @"\{[^}]+\}", "[^/]+");

            return new Regex("^" + regexPattern + "$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        });

        return regex.IsMatch(path);
    }

    private sealed record RouteEntry(string Method, string PathPattern, string PermissionCode);

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class RouteMappingConfig
    {
        public string Method { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }
}
