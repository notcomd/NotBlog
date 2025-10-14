using Markdown.Domain.Entities;

using NotMediator;
namespace Markdown.Domain.DomainEvent;


public class CreateMarkDownBlockDomainEvent : INotifications
{
    public CreateMarkDownBlockDomainEvent(Guid markDownGuid, int blockIndex, BlockType blockType, string? markDownMeta, DateTime createTime, DateTime updateTime, string? markDownContent)
    {
        MarkDownGuid = markDownGuid;
        BlockIndex = blockIndex;
        BlockType = blockType;
        MarkDownMeta = markDownMeta;
        CreateTime = createTime;
        UpdateTime = updateTime;
        MarkDownContent = markDownContent;
    }

    public Guid MarkDownGuid { get; }

    public int BlockIndex { get; }

    public BlockType BlockType { get; }

    public string? MarkDownMeta { get; }

    public DateTime CreateTime { get; }

    public DateTime UpdateTime { get; }

    public string? MarkDownContent { get; }


}
