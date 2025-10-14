using Markdown.Domain.Entities;

using NotMediator;

namespace Markdown.Domain.DomainEvent
{
    public class CreateMarkDownBlockVersionDomainEvent :INotifications
    {
        public CreateMarkDownBlockVersionDomainEvent(Guid blockVersionId, Guid userId, Guid markDownGuid, Guid markDownBlockGuid, int blockIndex, BlockType blockType, string markDownContent, string changeMessage, DateTime saveTime)
        {
            BlockVersionId = blockVersionId;
            UserId = userId;
            MarkDownGuid = markDownGuid;
            MarkDownBlockGuid = markDownBlockGuid;
            BlockIndex = blockIndex;
            BlockType = blockType;
            MarkDownContent = markDownContent;
            ChangeMessage = changeMessage;
            SaveTime = saveTime;
        }

        public Guid BlockVersionId { get; }

        public Guid UserId { get; private set; }

        public Guid MarkDownGuid { get; private set; }

        public Guid MarkDownBlockGuid { get; private set; }

        public int BlockIndex { get; private set; }

        public BlockType BlockType { get; private set; }

        public string MarkDownContent { get; private set; } = null!;

        public string ChangeMessage { get; private set; } = null!;

        public DateTime SaveTime { get; private set; }


    }
}
