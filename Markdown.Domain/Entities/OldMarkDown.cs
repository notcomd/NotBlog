namespace Markdown.Domain.Entities;

/// <summary>
///     MarkDown 历史版本记录（旧文档）
///     用于保存每次更新前的文档快照，支持版本回溯和审计
/// </summary>
public class OldMarkDown : Entity
{
    
     /// <summary>
    ///     历史版本唯一标识
    /// </summary>
    public Guid OldMarkDownGuid { get; private set; }

    /// <summary>
    ///     关联的当前文档 GUID（外键）
    /// </summary>
    public Guid MarkDownGuid { get; private set; }

    /// <summary>
    ///     创建/修改用户 GUID
    /// </summary>
    public Guid UserGuid { get; private set; }

    /// <summary>
    ///     历史版本的权限状态
    /// </summary>
    public MarkDownAuth Status { get; private set; }

    /// <summary>
    ///     历史版本文档内容
    /// </summary>
    public string OldMarkDownContent { get; private set; }

    /// <summary>
    ///     历史版本文档哈希值（用于版本比对）
    /// </summary>
    public string OldMarkDownHash { get; private set; }

    /// <summary>
    ///     是否已删除（软删除标记）
    /// </summary>
    public bool IsDelete { get; private set; }

    /// <summary>
    ///     历史版本创建时间
    /// </summary>
    public DateTimeOffset CreateAt { get; private set; }

    /// <summary>
    ///     历史版本更新时间（归档时间）
    /// </summary>
    public DateTimeOffset UpdateAt { get; private set; }

    /// <summary>
    ///     导航属性：关联的当前文档
    /// </summary>
    public virtual MarkDown? MarkDown { get; private set; }


    /// <summary>
    ///     更新历史版本记录（仅在需要时调用）
    /// </summary>
    /// <param name="content">新的内容</param>
    /// <param name="hash">新的哈希值</param>
    /// <param name="authType">新的权限类型</param>
    /// <summary>
    ///     私有默认构造函数，供 EF Core 使用
    /// </summary>
    private OldMarkDown()
    {
        OldMarkDownGuid = Guid.CreateVersion7();
        CreateAt = DateTimeOffset.UtcNow;
        UpdateAt = DateTimeOffset.UtcNow;
        IsDelete = false;
    }

    /// <summary>
    ///     创建历史版本记录
    /// </summary>
    /// <param name="markDownGuid">关联的当前文档 GUID</param>
    /// <param name="userGuid">创建/修改用户 GUID</param>
    /// <param name="content">历史版本文档内容</param>
    /// <param name="hash">历史版本文档哈希值</param>
    /// <param name="authType">历史版本的权限类型</param>
    public OldMarkDown(Guid markDownGuid, Guid userGuid, string content,
        string hash, MarkDownAuth authType):this()
    {
        MarkDownGuid = markDownGuid;
        UserGuid = userGuid;
        Status = authType;
        OldMarkDownContent = content ?? throw new ArgumentNullException(nameof(content));
        OldMarkDownHash = hash ?? throw new ArgumentNullException(nameof(hash));
    }

   
    internal void UpdateHistory(string content, string hash, MarkDownAuth authType)
    {
        OldMarkDownContent = content ?? throw new ArgumentNullException(nameof(content));
        OldMarkDownHash = hash ?? throw new ArgumentNullException(nameof(hash));
        Status = authType;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     标记为已删除（软删除）
    /// </summary>
    internal void SoftDelete()
    {
        IsDelete = true;
        UpdateAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     验证当前版本是否与指定哈希值匹配
    /// </summary>
    /// <param name="hash">要比较的哈希值</param>
    /// <returns>如果匹配返回 true</returns>
    public bool IsVersionMatch(string hash)
    {
        return OldMarkDownHash == hash;
    }

    /// <summary>
    ///     转换为当前文档的格式
    /// </summary>
    /// <returns>MarkDown 实例</returns>
    public MarkDown ToCurrentFormat()
    {
        // 创建一个只读的 MarkDown 实例用于查看
        var markdown = new MarkDown(
            UserGuid,
            $"历史版本_{OldMarkDownGuid}",
            OldMarkDownContent,
            OldMarkDownHash
        );
        // 使用反射或其他方式设置只读属性（如果需要完全还原）
        return markdown;
    }
}