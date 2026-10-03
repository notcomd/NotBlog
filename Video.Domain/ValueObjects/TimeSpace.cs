namespace Video.Domain.ValueObjects;

/// <summary>
/// 时间戳值对象 — 记录创建时间与最后更新时间（嵌入各实体/值对象）。
/// </summary>
public record TimeSpace
{

    /// <summary>以当前 UTC 时间初始化创建时间与更新时间。</summary>
    public TimeSpace()
    {
        CreateAt = DateTimeOffset.UtcNow;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>创建时间</summary>
    public DateTimeOffset CreateAt { get; init; }

    /// <summary>最后更新时间</summary>
    public DateTimeOffset UpdateAt { get; private set; }

    /// <summary>以指定的创建时间与更新时间构造。</summary>
    /// <param name="createAt">创建时间</param>
    /// <param name="updateAt">更新时间</param>
    public TimeSpace(DateTimeOffset createAt, DateTimeOffset updateAt)
    {
        CreateAt = createAt;
        UpdateAt = updateAt;
    }

    /// <summary>刷新最后更新时间。</summary>
    /// <param name="updateAt">新的更新时间</param>
    public void ResetUpdateAt(DateTimeOffset updateAt)
    {
        UpdateAt = updateAt;
    }
}