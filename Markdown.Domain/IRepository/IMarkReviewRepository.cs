

using DomainCommon;
using Markdown.Domain.Entities;

namespace Markdown.Domain.IRepository;

public interface IMarkReviewRepository : IRepository<MarkReview>
{
    /// <summary>
    /// 根据MarkDownGroupId获取MarkDownGroup实体的MarkDown列表
    /// </summary>
    /// <param name="markDownGroupId">MarkDownGroup实体的ID</param>
    /// <returns>MarkDownGroup实体的MarkDown列表</returns>
    Task<List<MarkReview>> GetMarkReviewsByMarkDownGroupIdAsync(Guid markDownGroupId);


    /// <summary>
    /// 根据MarkDownUserId获取MarkDownGroup实体的MarkDown列表
    /// </summary>
    /// <param name="markDownUserId">MarkDownUser实体的ID</param>
    /// <returns>MarkDownGroup实体的MarkDown列表</returns>
    Task<List<MarkReview>> GetMarkReviewsByMarkDownUserIdAsync(Guid markDownUserId);


    /// <summary>
    /// 根据MarkDownBlockId获取MarkDownGroup实体的MarkDown列表
    /// </summary>
    /// <param name="markDownBlockId">MarkDownBlock实体的ID</param>
    /// <returns>MarkDownGroup实体的MarkDown列表</returns>
    Task<List<MarkReview>> GetMarkReviewsByMarkDownBlockIdAsync(Guid markDownBlockId);
}
