using FileDev.Domain.Entities;

namespace FileDev.Domain.IRepository;

/// <summary>
/// 标签仓储（替代原文件组仓储）。标签为扁平一层，用户内按名称唯一约束。
/// </summary>
public interface INotFileTagRepository : IRepository<NotFileTag, IUnitOfWork>
{
    /// <summary>插入标签。</summary>
    Task InsertNotFileTagAsync(NotFileTag notFileTag);

    /// <summary>根据 ID 获取未删除的标签。</summary>
    Task<NotFileTag> GetNotFileTagByIdAsync(Guid tagId);

    /// <summary>获取用户的所有未删除标签（按创建时间升序）。</summary>
    Task<IEnumerable<NotFileTag>> GetNotFileTagsByUserIdAsync(Guid userId);

    /// <summary>按名称获取用户标签（未删除），不存在返回 null。</summary>
    Task<NotFileTag?> GetNotFileTagByNameAsync(Guid userId, string tagName);

    /// <summary>按默认标签种类获取用户标签（未删除），用于上传文件自动归类；不存在返回 null。</summary>
    Task<NotFileTag?> GetNotFileTagByKindAsync(Guid userId, FileDev.Domain.Enum.TagDefaultKind kind);

    /// <summary>判断用户下是否存在同名标签（排除自身，用于改名场景）。</summary>
    Task<bool> ExistsByUserIdAndNameAsync(Guid userId, string tagName, Guid? excludeId = null);

    /// <summary>更新标签（标记 Modified，由 SaveChangesAsync 持久化）。</summary>
    Task<NotFileTag?> UpdateNotFileTagAsync(NotFileTag notFileTag);
}