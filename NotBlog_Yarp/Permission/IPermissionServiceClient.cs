namespace NotBlog_Yarp.Permission;

/// <summary>
/// 权限检查 + DataScope 组合查询结果
/// </summary>
public sealed record PermissionCheckResult
{
    /// <summary>是否拥有指定权限</summary>
    public bool HasPermission { get; private init; }

    /// <summary>数据范围序列化值（格式: "type|value1,value2,..."），默认 "0|"</summary>
    public Dictionary<string, HashSet<string>> DataScope { get; private init; } = new() { { "0", new HashSet<string>() } };

    /// <summary>权限被拒绝</summary>
    public static PermissionCheckResult Denied() => new() { HasPermission = false };

    /// <summary>权限通过 + 指定 DataScope</summary>
    public static PermissionCheckResult Granted(Dictionary<string, HashSet<string>> dataScope) => new()
    {
        HasPermission = true,
        DataScope = dataScope
    };

    /// <summary>权限通过 + 默认 Own DataScope</summary>
    public static PermissionCheckResult GrantedOwn() => new()
    {
        HasPermission = true,
        DataScope = new Dictionary<string, HashSet<string>>() { { "0", new HashSet<string>() } }
    };
}

/// <summary>
/// 权限路由映射 DTO — 与 Identity 侧的 PermissionMappingDto 结构一致
/// </summary>
public sealed record PermissionMappingDto
{
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// 权限服务客户端接口 — 网关侧调用 Identity 进行权限校验的抽象
/// 
/// 定义此接口使网关与 Identity 的具体通信方式（HTTP/gRPC/内存）解耦，
/// 便于测试、替换和扩展。
/// </summary>
public interface IPermissionServiceClient
{
    /// <summary>
    /// 检查用户是否拥有指定权限编码
    /// </summary>
    Task<bool> CheckPermissionAsync(Guid userId, string permissionCode, CancellationToken ct = default);

    /// <summary>
    /// 获取用户的数据访问范围
    /// </summary>
    Task<string> GetDataScopeAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// 组合查询：同时检查权限和获取数据范围，减少一次网络往返
    /// 默认实现回退到两次独立调用；子类可覆写为单次调用
    /// </summary>
   async Task<PermissionCheckResult> CheckAndGetScopeAsync(
    Guid userId, string permissionCode, CancellationToken ct = default)
{
    var hasPermission = await CheckPermissionAsync(userId, permissionCode, ct);
    if (!hasPermission)
        return PermissionCheckResult.Denied();

    var dataScope = await GetDataScopeAsync(userId, ct);
    if (string.IsNullOrWhiteSpace(dataScope))
        return PermissionCheckResult.Denied();

    var parts = dataScope.Split('|', 2);
    if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]))
    {
        // 格式无效，根据业务可返回 Denied 或 GrantedOwn，或记录日志
        return PermissionCheckResult.Denied(); // 或 GrantedOwn()
    }

    var values = parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries);
    var scopeDict = new Dictionary<string, HashSet<string>>
    {
        { parts[0], new HashSet<string>(values) }
    };
    return PermissionCheckResult.Granted(scopeDict);
}

    /// <summary>
    /// 获取全部 URL→PermissionCode 路由映射（网关注入时调用）
    /// 默认返回空列表；HttpPermissionServiceClient 覆写为从 Identity 拉取
    /// </summary>
    Task<IReadOnlyList<PermissionMappingDto>> GetMappingsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<PermissionMappingDto>>(Array.Empty<PermissionMappingDto>());
}
