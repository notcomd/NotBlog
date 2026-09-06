namespace Notcomd.Token.JWT.Core;

/// <summary>
/// 权限码前缀段匹配器（服务内端点判定与网关本地判定共用同一实现，保证语义一致）
/// </summary>
public static class PermissionCodeMatcher
{
    /// <summary>
    /// 判定授权码集合是否覆盖目标权限码（目录授权自动覆盖子孙）：
    /// code == granted || code.StartsWith(granted + ":", OrdinalIgnoreCase)
    /// </summary>
    public static bool HasPermission(IReadOnlyCollection<string> grantedCodes, string requestedCode)
    {
        if (grantedCodes is null || grantedCodes.Count == 0 || string.IsNullOrWhiteSpace(requestedCode))
            return false;

        if (grantedCodes.Contains(requestedCode))
            return true;

        return grantedCodes.Any(granted =>
            !string.IsNullOrWhiteSpace(granted)
            && requestedCode.StartsWith(granted + ":", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 从逗号分隔的权限 claim 值解析授权码集合
    /// </summary>
    public static HashSet<string> ParsePermissionClaim(string? claimValue)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(claimValue))
            return set;

        foreach (var code in claimValue.Split(',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            set.Add(code);
        return set;
    }
}
