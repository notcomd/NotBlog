namespace Markdown.Domain.Entities;

/// <summary>
///     文章收藏（聚合根）：用户对文章的收藏记录，支持 tag 分类管理。
///     同一用户对同一文章仅能收藏一次（(UserGuid, MarkDownGuid) 唯一约束兜底并发）。
///     标签集合由 TagsJson 文本列持久化（JSON 数组），供 SQL LIKE 快速过滤查找。
/// </summary>
public class MarkFavorite : Entity<int>, IAggregateRoot
{
    /// <summary>单标签最大长度（与文章标签规则一致）</summary>
    public const int MaxTagLength = 50;

    /// <summary>标签数量上限（与文章标签规则一致）</summary>
    public const int MaxTagCount = 20;

    private HashSet<string> _tags = [];

    private MarkFavorite()
    {
        MarkFavoriteGuid = Guid.CreateVersion7();
        CreateAt = DateTimeOffset.UtcNow;
        TagsJson = "[]";
    }

    private MarkFavorite(Guid userGuid, Guid markDownGuid, IEnumerable<string> tags) : this()
    {
        UserGuid = userGuid;
        MarkDownGuid = markDownGuid;
        SetTags(tags);
    }

    /// <summary>
    ///     创建收藏（聚合根统一入口）
    /// </summary>
    /// <param name="userGuid">收藏用户 GUID</param>
    /// <param name="markDownGuid">被收藏文章 GUID</param>
    /// <param name="tags">收藏分类标签（可选，空白标签忽略）</param>
    /// <exception cref="ArgumentException">用户/文章标识为空或标签非法时抛出</exception>
    public static MarkFavorite Create(Guid userGuid, Guid markDownGuid, IEnumerable<string>? tags)
    {
        if (userGuid == Guid.Empty)
            throw new ArgumentException("用户标识不能为空", nameof(userGuid));
        if (markDownGuid == Guid.Empty)
            throw new ArgumentException("文章标识不能为空", nameof(markDownGuid));

        return new MarkFavorite(userGuid, markDownGuid, tags ?? []);
    }

    public Guid MarkFavoriteGuid { get; init; }

    /// <summary>
    ///     收藏用户 GUID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    ///     被收藏文章 GUID
    /// </summary>
    public Guid MarkDownGuid { get; init; }

    /// <summary>
    ///     收藏时间
    /// </summary>
    public DateTimeOffset CreateAt { get; init; }

    /// <summary>
    ///     收藏标签（JSON 数组文本，持久化列，供 SQL LIKE 精确过滤）
    /// </summary>
    public string TagsJson { get; private set; } = "[]";

    /// <summary>
    ///     收藏标签集合（充血模型操作入口；EF 忽略映射，由 TagsJson 同步持久化）
    /// </summary>
    public IReadOnlyCollection<string> Tags => _tags;

    /// <summary>
    ///     整体设置标签（覆盖式）
    /// </summary>
    public void SetTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;
            var trimmed = tag.Trim();
            if (trimmed.Length > MaxTagLength)
                throw new ArgumentException($"标签长度不能超过 {MaxTagLength} 个字符");
            normalized.Add(trimmed);
        }

        if (normalized.Count > MaxTagCount)
            throw new ArgumentException($"收藏标签不能超过 {MaxTagCount} 个");

        _tags = normalized;
        SyncTagsJson();
    }

    /// <summary>
    ///     追加标签（已存在的标签自动去重；先全量校验再提交，避免部分更新）
    /// </summary>
    public void AddTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var additions = new List<string>();
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;
            var trimmed = tag.Trim();
            if (trimmed.Length > MaxTagLength)
                throw new ArgumentException($"标签长度不能超过 {MaxTagLength} 个字符");
            if (!_tags.Contains(trimmed))
                additions.Add(trimmed);
        }

        if (_tags.Count + additions.Count > MaxTagCount)
            throw new ArgumentException($"收藏标签不能超过 {MaxTagCount} 个");

        if (additions.Count == 0)
            return;

        foreach (var addition in additions)
            _tags.Add(addition);
        SyncTagsJson();
    }

    /// <summary>
    ///     追加单个标签
    /// </summary>
    public void AddTag(string tag) => AddTags([tag]);

    /// <summary>
    ///     移除标签
    /// </summary>
    /// <returns>成功移除返回 true，标签不存在返回 false</returns>
    public bool RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        if (_tags.Remove(tag.Trim()))
        {
            SyncTagsJson();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     清空所有标签
    /// </summary>
    public void ClearTags()
    {
        if (_tags.Count == 0)
            return;

        _tags.Clear();
        SyncTagsJson();
    }

    /// <summary>
    ///     是否包含指定标签
    /// </summary>
    public bool HasTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && _tags.Contains(tag.Trim());
    }

    /// <summary>
    ///     将当前标签集合同步到 TagsJson 持久化列
    /// </summary>
    private void SyncTagsJson()
    {
        TagsJson = System.Text.Json.JsonSerializer.Serialize(_tags);
    }
}
