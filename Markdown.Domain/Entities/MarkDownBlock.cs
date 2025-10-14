
using DomainCommon;



namespace Markdown.Domain.Entities;

public class MarkDownBlock : Entity
{
    public Guid MarkDownGuid { get; private set; }

    public int BlockIndex { get; private set; }

    public BlockType BlockType { get; private set; }

    public string MarkDownContent { get; private set; } = null!;

    public int MarkDownOrder { get; private set; }

    public string MarkDownText { get; private set; } = null!;

    public string? MarkDonwCodeLanguage { get; private set; }

    public string? MarkDownMeta { get; private set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }


    protected MarkDownBlock()
    {

    }

    public MarkDownBlock(Guid markDownGuid, int blockIndex, BlockType blockType, string markDownContent, int markDownOrder, string markDownText,
    string? markDonwCodeLanguage, string? markDownMeta, DateTime createTime, DateTime updateTime)
    {
        Id = Guid.CreateVersion7();
        MarkDownGuid = markDownGuid;
        BlockIndex = blockIndex;
        BlockType = blockType;
        MarkDownContent = markDownContent;
        MarkDownOrder = markDownOrder;
        MarkDownText = markDownText;
        MarkDonwCodeLanguage = markDonwCodeLanguage;
        MarkDownMeta = markDownMeta;
        CreateTime = createTime;
        UpdateTime = updateTime;
    }


}

