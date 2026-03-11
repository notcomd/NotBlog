namespace Markdown.Domain.Entities;

public class MarkReview
{
    /// <summary>
    /// </summary>
    private MarkReview()
    {
        this.MarkReviewGuid = Guid.CreateVersion7();
        this.MarkReviewTime = DateTime.UtcNow;
    }
    /// <summary>
    /// </summary>
    /// <param name="markDown"></param>
    /// <param name="userName"></param>
    /// <param name="userImage"></param>
    /// <param name="markReviewContent"></param>
    public MarkReview(MarkDown markDown, string userName, string userImage, string markReviewContent) : this()
    {
        MarkDownGuid = markDown.MarkDownGuid;
        MarkReviewContent = markReviewContent;
        UserName = userName;
        UserImage = userImage;
        MarkReviewTime = DateTime.Now;
        MarkDown = markDown;
    }
    /// <summary>
    /// 评论主键key
    /// </summary>
    public Guid MarkReviewGuid { get; init; }
    /// <summary>
    /// 文档的guid(外键）
    /// </summary>
    public Guid MarkDownGuid { get; init; }

    /// <summary>
    /// 用户Id
    /// </summary>
    public Guid UserId { get; init; }
    /// <summary>
    /// 子评论
    /// </summary>
    public Guid? MarkAggregateRootGuid { get; private set; }
    /// <summary>
    /// 用户明
    /// </summary>
    public string UserName { get; private set; } = null!;
    /// <summary>
    /// 用户头像
    /// </summary>
    public string UserImage { get; private set; } = null!;
    /// <summary>
    /// 评论主体
    /// </summary>
    public string MarkReviewContent { get; private set; } = null!;
    /// <summary>
    /// 时间
    /// </summary>
    public DateTime MarkReviewTime { get; private set; } = DateTime.Now;
    /// <summary>
    ///     默认评论为公开
    /// </summary>
    public MarkReviewAuth MarkReviewAuth { get; private set; } = MarkReviewAuth.ReviewAuthPublic;
    /// <summary>
    ///     外键关联
    /// </summary>
    public MarkDown MarkDown { get; private set; }




    private Task<MarkReview> AddToChildReviewAsync(Guid aggregateRootGuid, MarkReview markReview)
    {
        markReview.MarkAggregateRootGuid = aggregateRootGuid;
        return Task.FromResult(markReview);
    }

    public Task<MarkReview> UpDataByMarkReviewAuthAsync(MarkReviewAuth markReviewAuth)
    {
        MarkReviewAuth = markReviewAuth;
        return Task.FromResult(this);
    }
}