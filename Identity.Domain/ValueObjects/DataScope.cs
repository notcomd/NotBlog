namespace Identity.Domain.ValueObjects;

/// <summary>
/// 数据范围值对象 — 定义用户对业务数据的可见范围
/// 用于 JWT Claims 中传递，由下游服务解析执行行级过滤
/// </summary>
public sealed class DataScope : ValueObject
{
    private DataScope()
    {
        Values = new HashSet<string>();
    }

    public DataScope(DataScopeType type, IReadOnlySet<string>? values = null)
    {
        Type = type;
        Values = values ?? new HashSet<string>();
    }

    /// <summary>范围类型</summary>
    public DataScopeType Type { get; private init; }

    /// <summary>范围值（如部门ID列表、租户ID列表）</summary>
    public IReadOnlySet<string> Values { get; private init; }

    public bool IsAll => Type == DataScopeType.All;
    public bool IsOwn => Type == DataScopeType.Own;

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return Type;
        foreach (var v in Values.OrderBy(x => x))
            yield return v;
    }

    /// <summary>序列化为 JWT Claim 值，格式: "type|value1,value2,..."</summary>
    public string ToClaimValue()
    {
        return $"{(int)Type}|{string.Join(",", Values)}";
    }

    /// <summary>从 Claim 值反序列化</summary>
    public static DataScope Parse(string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
            return new DataScope(DataScopeType.Own, new HashSet<string>());

        var parts = claimValue.Split('|', 2);
        if (!int.TryParse(parts[0], out var typeInt))
            return new DataScope(DataScopeType.Own, new HashSet<string>());

        var type = (DataScopeType)typeInt;
        var values = parts.Length > 1 && !string.IsNullOrEmpty(parts[1])
            ? new HashSet<string>(parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
            : new HashSet<string>();

        return new DataScope(type, values);
    }

    /// <summary>创建仅本人可见的数据范围</summary>
    public static DataScope Own() => new(DataScopeType.Own);

    /// <summary>创建全部可见的数据范围</summary>
    public static DataScope All() => new(DataScopeType.All);

    /// <summary>创建部门范围</summary>
    public static DataScope Department(IReadOnlySet<string> deptIds) => new(DataScopeType.Department, deptIds);
}

public enum DataScopeType
{
    /// <summary>仅自己的数据</summary>
    Own = 0,

    /// <summary>部门范围</summary>
    Department = 1,

    /// <summary>全部数据</summary>
    All = 2
}
