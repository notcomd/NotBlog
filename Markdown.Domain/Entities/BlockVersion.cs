
using DomainCommon;

using Markdown.Domain.DomainEvent;

namespace Markdown.Domain.Entities;

public class BlockVersion : Entity, IAggregateRoot
{
    protected BlockVersion()
    {

    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="markDownBlockGuid">MarkDown块ID</param>
    /// <param name="blockIndex">块索引</param>
    /// <param name="blockType">块类型</param>
    /// <param name="markDownContent">MarkDown内容</param>
    /// <param name="changeMessage">变更消息</param>
    /// <param name="saveTime">保存时间</param>
    public BlockVersion(Guid userId, Guid markDownGuid, Guid markDownBlockGuid, int blockIndex, BlockType blockType,
    string markDownContent, string changeMessage, DateTime saveTime, MarkQuote? markQuote = null)
    {

        Id = Guid.CreateVersion7();
        MarkDownGuid = markDownGuid;
        MarkQuote = markQuote;
        UserId = userId;
        MarkDownBlockGuid = markDownBlockGuid;
        BlockIndex = blockIndex;
        BlockType = blockType;
        MarkDownContent = markDownContent;
        ChangeMessage = changeMessage;
        SaveTime = saveTime;

        AddDomainEvent(new CreateMarkDownBlockVersionDomainEvent(Id, UserId, MarkDownGuid, MarkDownBlockGuid, BlockIndex, BlockType, MarkDownContent, ChangeMessage, SaveTime));
    }

    public Guid UserId { get; private set; }

    public Guid MarkDownGuid { get; private set; }

    public Guid MarkDownBlockGuid { get; private set; }

    public int BlockIndex { get; private set; }

    public BlockType BlockType { get; private set; }

    public string MarkDownContent { get; private set; } = null!;

    public string ChangeMessage { get; private set; } = null!;

    public ICollection<Guid> MarkReviews { get; } = new HashSet<Guid>();

    public MarkQuote? MarkQuote { get; private set; } = null!;

    public DateTime SaveTime { get; private set; }



    public void AddMarkReview(Guid reviewId)
    {
        MarkReviews.Add(reviewId);
    }


    public void SetMarkQuote(MarkQuote markQuote)
    {
        MarkQuote = markQuote;
    }

    public void RemoveMarkQuote()
    {
        MarkQuote = null;
    }

    public void UpdateContent(string newContent, string changeMessage, DateTime saveTime)
    {
        MarkDownContent = newContent;
        ChangeMessage = changeMessage;
        SaveTime = saveTime;
    }

    public void UpdateBlockType(BlockType newType)
    {
        BlockType = newType;
    }

    public void UpdateBlockIndex(int newIndex)
    {
        BlockIndex = newIndex;
    }



}