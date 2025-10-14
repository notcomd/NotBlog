

using System.Net.NetworkInformation;
using DomainCommon;

namespace Markdown.Domain.Entities;

/// <summary>
/// 评论类，用于表示对文档的评论信息
/// </summary>
public class MarkReview : Entity, IAggregateRoot
{
    /// <summary>
    /// 初始化评论类的新实例，仅供ORM使用
    /// </summary>
    private MarkReview() { }

    /// <summary>
    /// 初始化评论类的新实例
    /// </summary>
    /// <param name="markDown">关联的文档对象</param>
    /// <param name="userName">评论用户的名称</param>
    /// <param name="markReviewContent">评论内容</param>
    /// <param name="parentReviewId">父评论ID，若为顶级评论则为 null</param>
    /// <param name="auth">评论的权限设置，默认为公开</param>
    public MarkReview(Guid userGuid, Guid markDownGuid, string markReviewContent,
                     Guid? parentReviewId = null, MarkQuote? markQuote = null, MarkReviewType auth = MarkReviewType.ReviewAuthPublic)
    {
        Id = Guid.CreateVersion7();
        MarkDownGuid = markDownGuid;
        UserGuid = userGuid;
        MarkReviewContent = markReviewContent;
        MarkReviewTime = DateTime.UtcNow;
        MarkQuote = markQuote;
        MarkAggregateRootGuid = parentReviewId;
        MarkReviewType = auth;
    }



    public Guid MarkDownGuid { get; init; }

    public Guid UserGuid { get; init; }

    public MarkReviewType MarkReviewType { get; private set; }

    public string MarkReviewContent { get; private set; } = null!;

    public MarkQuote? MarkQuote { get; private set; }    

    public DateTime MarkReviewTime { get; private set; }

    public Guid? MarkAggregateRootGuid { get; private set; }

    public ICollection<MarkReview> MarkChildReviews { get; } = new List<MarkReview>();



    /// <summary>
    /// 添加子评论
    /// </summary>
    /// <param name="childReview">待添加的子评论</param>
    /// <returns>添加了父评论ID的子评论对象</returns>
    public MarkReview AddChildReview(MarkReview childReview)
    {
        childReview.MarkAggregateRootGuid = this.Id;
        return childReview;
    }

    /// <summary>
    /// 更新评论的权限设置
    /// </summary>
    /// <param name="markReviewAuth">新的评论权限设置</param>
    public void UpdateReviewAuth(MarkReviewType markReviewAuth)
    {
        MarkReviewType = markReviewAuth;
    }


    /// <summary>
    /// 更新评论内容
    /// </summary>
    /// <param name="markReviewContent">新的评论内容</param>
    public void UpdateReviewContent(string markReviewContent)
    {
        MarkReviewContent = markReviewContent;
    }

}