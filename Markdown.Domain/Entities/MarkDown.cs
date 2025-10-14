using DomainCommon;

using Markdown.Domain.DomainEvent;

namespace Markdown.Domain.Entities;


public class MarkDown : Entity, IAggregateRoot
{
    /// <summary>
    ///  文档用户Guid
    /// </summary>
    public Guid MarkDownUserGuid { get; private set; }

    /// <summary>
    ///  文档名称
    /// </summary>
    public string MarkDownName { get; private set; } = null!;

    /// <summary>
    /// 文档哈希值
    /// </summary>
    public string MarkDownHash { get; private set; } = null!;

    /// <summary>
    /// 文档内容
    /// </summary>
    public string MarkDownContent { get; private set; } = null!;

    /// <summary>
    /// 文档引用
    /// </summary>
    public MarkQuote? MarkQuote { get; private set; } = null!;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateAt { get; init; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime UplaodAt { get; private set; }

    /// <summary>
    /// 文档块
    /// </summary>
    public ICollection<MarkDownBlock> MarkDownBlocks { get; private set; } = new HashSet<MarkDownBlock>();

    /// <summary>
    /// 文档评论
    /// </summary>
    public ICollection<Guid>? MarkReviews { get; private set; } = new HashSet<Guid>();

    /// <summary>
    /// 文档标签
    /// </summary>
    public ICollection<string>? MarkDownTagboard { get; private set; } = new HashSet<string>();



    protected MarkDown() { }

    /// <summary>
    /// 初始化MarkDown实体
    /// </summary>
    /// <param name="markDownGuid">文档唯一标识</param>
    /// <param name="markDownName">文档名称</param>
    /// <param name="markDownTagboard">文档标签</param>
    /// <param name="markOption">文档选项</param>
    /// <param name="markDownHash">文档哈希值</param>
    /// <param name="markDownContent">文档内容</param>
    /// <param name="uploadAt">上传时间</param>
    public MarkDown(Guid markDownUserGuid, string markDownName, List<string>? markDownTagboard,
             string markDownHash, string markDownContent, MarkQuote? markQuote = null, DateTime createAt = default, DateTime uploadAt = default)
    {
        Id = Guid.CreateVersion7();
        MarkDownUserGuid = markDownUserGuid;
        MarkDownName = markDownName;
        MarkQuote = markQuote;
        MarkDownTagboard = markDownTagboard ?? new List<string>();
        MarkDownHash = markDownHash;
        MarkDownContent = markDownContent;
        CreateAt = createAt == default ? DateTime.UtcNow : createAt;
        UplaodAt = uploadAt == default ? DateTime.UtcNow : uploadAt;
        MarkReviews = new HashSet<Guid>();
        MarkDownBlocks = new HashSet<MarkDownBlock>();
        AddDomainEvent(new CreateMarkDownDomainEvent(Id, MarkDownUserGuid, MarkDownName, MarkDownTagboard, MarkDownHash, MarkDownContent, CreateAt, UplaodAt));
    }



    /// <summary>
    /// 异步添加MarkReview
    /// </summary>
    /// <param name="markReview">要添加的MarkReview实体</param>
    public Task AddMarkReviewAsync(Guid markReview)
    {
        MarkReviews!.Add(markReview);
        return Task.FromResult(this);
    }

    /// <summary>
    /// 异步移除MarkReview
    /// </summary>
    /// <param name="markReview">要移除的MarkReview实体</param>
    /// <returns>当前MarkDown实体</returns>
    public Task RemoveMarkReviewAsync(Guid markReview)
    {
        MarkReviews!.Remove(markReview);
        return Task.FromResult(this);
    }

    /// <summary>
    /// 更新文档名称
    /// </summary>
    /// <param name="newName">新的文档名称</param>
    public void UpdateMarkDownName(string newName)
    {
        MarkDownName = newName;
    }

    /// <summary>
    /// 更新文档标签
    /// </summary>
    /// <param name="newTagboard">新的文档标签列表</param>
    public void UpdateMarkDownTagboard(List<string>? newTagboard)
    {
        MarkDownTagboard = newTagboard ?? new List<string>();
    }



    /// <summary>
    /// 更新文档内容
    /// </summary>
    /// <param name="newContent">新的文档内容</param>
    /// <param name="newHash">新的文档哈希值</param>
    /// <param name="newUploadAt">新的上传时间</param>
    public void UpdateMarkDownContent(string newContent, string newHash, DateTime newUploadAt)
    {
        MarkDownContent = newContent;
        MarkDownHash = newHash;
        UplaodAt = newUploadAt;
    }

    /// <summary>
    /// 获取MarkDown实体的MarkReview列表
    /// </summary>
    /// <returns>MarkDown实体的MarkReview列表</returns>
    public List<Guid> GetMarkReviews()
    {
        return (List<Guid>)(MarkReviews ?? new HashSet<Guid>());
    }




}