using Markdown.Domain.Entities;
using Markdown.Domain.SeedWork;

namespace Markdown.Domain.IRepository;

/// <summary>
///     MarkReview 仓储接口
/// </summary>
public interface IMarkReviewRepository : IRepository<MarkDown>
{
    /// <summary>
    ///     根据 Markdown ID 获取所有评论
    /// </summary>
    Task<IEnumerable<MarkReview>> GetReviewsByMarkdownIdAsync(Guid markdownGuid);

    /// <summary>
    ///     根据评论 ID 获取评论
    /// </summary>
    Task<MarkReview?> GetReviewByIdAsync(Guid reviewGuid);

    /// <summary>
    ///     获取某条评论的所有子评论
    /// </summary>
    Task<IEnumerable<MarkReview>> GetChildReviewsAsync(Guid aggregateRootGuid);

    /// <summary>
    ///     添加评论
    /// </summary>
    Task AddReviewAsync(MarkReview review);

    /// <summary>
    ///     删除评论（包括级联删除子评论）
    /// </summary>
    Task DeleteReviewAsync(MarkReview review);

    /// <summary>
    ///     添加子评论到父评论
    /// </summary>
    Task AddChildReviewAsync(Guid parentReviewGuid, MarkReview childReview);
}