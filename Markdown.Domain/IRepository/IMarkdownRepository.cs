
namespace Markdown.Domain.IRepository;

/// <summary>
///     MarkDown 聚合根仓储接口（唯一对外暴露的仓储，所有聚合内实体的操作必须通过此接口）
/// </summary>
public interface IMarkdownRepository : IRepository<MarkDown>
{
    /// <summary>
    ///     获取追踪状态的 MarkDown 实体（用于更新操作）
    /// </summary>
    Task<MarkDown?> GetMarkDownTrackedAsync(Guid markDownGuid);

    Task<MarkDown?> FindMarkDownAsync(Guid markDownGuid);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(Guid userGuid);

    Task<MarkDown?> FindMarkDownAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(string markDownName);

    Task<IEnumerable<MarkDown>?> FindMarkDownsAsync(MarkDownAuth markDownAuth);

    // ===== 评论相关查询（通过聚合根 MarkDown 访问，不暴露 MarkReview 独立仓储） =====

    /// <summary>
    ///     根据 Markdown ID 获取所有顶级评论（通过聚合根导航属性查询）
    /// </summary>
    Task<IEnumerable<MarkReview>> GetReviewsByMarkdownIdAsync(Guid markdownGuid);

    /// <summary>
    ///     根据评论 ID 获取评论（通过聚合根查找）
    /// </summary>
    Task<MarkReview?> GetReviewByIdAsync(Guid reviewGuid);

    /// <summary>
    ///     获取某条评论的所有子评论（通过聚合根查找）
    /// </summary>
    Task<IEnumerable<MarkReview>> GetChildReviewsAsync(Guid parentReviewGuid);

    // ===== 聚合根持久化操作（增删改） =====

    /// <summary>
    ///     添加新的 MarkDown 聚合根
    /// </summary>
    Task<MarkDown> AddAsync(MarkDown markDown);

    /// <summary>
    ///     更新 MarkDown 聚合根
    /// </summary>
    Task<MarkDown> UpdateAsync(MarkDown markDown);

    /// <summary>
    ///     删除 MarkDown 聚合根（软删除，通过聚合根方法执行）
    /// </summary>
    Task DeleteAsync(MarkDown markDown);

    /// <summary>
    ///     根据 GUID 删除 MarkDown 聚合根（软删除）
    /// </summary>
    Task DeleteAsync(Guid markDownGuid);
}
