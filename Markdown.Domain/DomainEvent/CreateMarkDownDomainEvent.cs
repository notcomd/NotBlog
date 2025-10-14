
using NotMediator;

namespace Markdown.Domain.DomainEvent
{
    /// <summary>
    /// 创建MarkDown域事件
    /// </summary>
    public class CreateMarkDownDomainEvent : INotifications
    {
        public CreateMarkDownDomainEvent(Guid markDownGuid, Guid markDownUserGuid, string markDownName, ICollection<string>? markDownTagboard, string markDownHash, string markDownContent, DateTime createAt, DateTime uplaodAt)
        {
            MarkDownGuid = markDownGuid;
            MarkDownUserGuid = markDownUserGuid;
            MarkDownName = markDownName;
            MarkDownTagboard = markDownTagboard;
            MarkDownHash = markDownHash;
            MarkDownContent = markDownContent;
            CreateAt = createAt;
            UplaodAt = uplaodAt;
        }

        public Guid MarkDownGuid { get; private set; }

        public Guid MarkDownUserGuid { get; private set; }

        public string MarkDownName { get; private set; } = null!;

        public ICollection<string>? MarkDownTagboard { get; private set; } = new HashSet<string>();

        public string MarkDownHash { get; private set; } = null!;

        public string MarkDownContent { get; private set; } = null!;

        public DateTime CreateAt { get; init; }
        
        public DateTime UplaodAt { get; private set; }

     
    }
}