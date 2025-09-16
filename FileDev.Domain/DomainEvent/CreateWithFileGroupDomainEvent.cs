using NotMediator;

namespace FileDev.Domain.DomainEvent;

public class CreateWithFileGroupDomainEvent : INotifications
{
    public string FileGroupName { get; set; }

    public Guid FileGroupBelongToUserGuid { get; set; }

    public List<string> FileGroupTags { get; set; } = new List<string>();

    public DateTime CreateWithFileGroupDate { get; set; }

}
