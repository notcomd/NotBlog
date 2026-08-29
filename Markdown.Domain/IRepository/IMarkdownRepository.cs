
namespace Markdown.Domain.IRepository;

/// <summary>
///     MarkDown 聚合根仓储接口（唯一对外暴露的仓储，所有聚合内实体的操作必须通过此接口）
/// </summary>
public interface IMarkdownRepository : IRepository<MarkDown, IUnitOfWork>
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

    // ===== OldMarkDown 历史版本查询（通过聚合根导航属性访问） =====

    /// <summary>
    ///     获取指定文档的所有历史版本
    /// </summary>
    Task<IEnumerable<OldMarkDown>> GetOldMarkDownsByMarkDownGuidAsync(Guid markDownGuid);

    /// <summary>
    ///     根据 GUID 获取单个历史版本
    /// </summary>
    Task<OldMarkDown?> GetOldMarkDownByGuidAsync(Guid oldMarkDownGuid);

    /// <summary>
    ///     软删除历史版本记录
    /// </summary>
    Task DeleteOldMarkDownAsync(Guid oldMarkDownGuid);

    // ===== 评论持久化操作（通过聚合根） =====

    /// <summary>
    ///     通过聚合根添加评论
    /// </summary>
    Task<MarkReview> AddReviewAsync(Guid markDownGuid, MarkReview review);

    /// <summary>
    ///     更新评论内容
    /// </summary>
    Task<MarkReview> UpdateReviewAsync(Guid reviewGuid, string content);

    /// <summary>
    ///     软删除评论（通过聚合根）
    /// </summary>
    Task DeleteReviewAsync(Guid reviewGuid);

    // ===== 列表查询 =====

    /// <summary>
    ///     获取所有非删除、已审核通过、公开的 Markdown 文档（分页）
    /// </summary>
    Task<IEnumerable<MarkDown>> FindAllMarkDownsAsync(int skip = 0, int take = 20);

    /// <summary>
    ///     通过聚合根添加子评论到指定父评论（F-10.4）
    /// </summary>
    Task<MarkReview> AddChildReviewAsync(Guid markDownGuid, Guid parentReviewGuid, MarkReview childReview);

    /// <summary>
    ///     评论点赞 +1（同一用户对同一评论仅能点赞一次，唯一约束防并发重复），返回计数与是否首次
    /// </summary>
    Task<InteractionResult> LikeReviewAsync(Guid reviewGuid, Guid userId);

    /// <summary>
    ///     取消评论点赞 -1（不低于 0，未点赞时幂等返回当前计数），返回最新点赞数（F-10.5）
    /// </summary>
    Task<long> RemoveLikeReviewAsync(Guid reviewGuid, Guid userId);

    /// <summary>
    ///     评论浏览量 +1（线程安全），返回最新浏览数（F-10.5）
    /// </summary>
    Task<long> IncreaseReviewViewAsync(Guid reviewGuid);

    // ===== 文档交互计数（文件化重构后新增，收藏命令事务内调用） =====

    /// <summary>
    ///     更新文档收藏计数（收藏 +1 / 取消收藏 -1，下限钳制 0）。
    ///     不单独提交，由调用方与收藏记录变更在同一 SaveChanges 内提交，保证计数与记录原子一致
    /// </summary>
    Task UpdateFavoriteCountAsync(Guid markDownGuid, long delta);

    // ===== 文档交互计数（阶段 2 端点：浏览/点赞/分享/硬币） =====

    /// <summary>
    ///     文档点赞 +1（同一用户对同一文档仅能点赞一次，唯一约束防并发重复），返回计数与是否首次
    /// </summary>
    Task<InteractionResult> LikeDocumentAsync(Guid markDownGuid, Guid userId);

    /// <summary>
    ///     取消文档点赞 -1（不低于 0，未点赞时幂等返回当前计数），返回最新点赞数
    /// </summary>
    Task<long> RemoveLikeDocumentAsync(Guid markDownGuid, Guid userId);

    /// <summary>
    ///     文档浏览量 +1（原子 SQL 更新），返回最新浏览数
    /// </summary>
    Task<long> IncreaseDocumentViewAsync(Guid markDownGuid);

    /// <summary>
    ///     文档分享 +1，返回最新分享数
    /// </summary>
    Task<long> AddDocumentShareAsync(Guid markDownGuid);

    /// <summary>
    ///     文档打赏硬币（一用户一文档一次：记录 MarkCoin 流水 + 计数增加），重复投币幂等返回现总额，返回计数与是否首次
    /// </summary>
    Task<InteractionResult> CoinDocumentAsync(Guid markDownGuid, Guid userId, long amount);

    // ===== 评论踩（阶段 2 端点） =====

    /// <summary>
    ///     评论踩 +1（同一用户对同一评论仅能踩一次，唯一约束防并发重复），返回计数与是否首次
    /// </summary>
    Task<InteractionResult> DislikeReviewAsync(Guid reviewGuid, Guid userId);

    /// <summary>
    ///     取消评论踩 -1（不低于 0，未踩时幂等返回当前计数），返回最新踩数
    /// </summary>
    Task<long> RemoveDislikeReviewAsync(Guid reviewGuid, Guid userId);
}
