
using NotMediator;

namespace FileDev.Domain.DomainEvent;

    public class CreateFileRepositoryDomainEvent : INotifications
{

    public Guid FileRepositoryId { get; }

    public string FileRepositoryName { get; }

    public string FileRepositoryVersion { get; }

    public List<string> FileRepositoryTags { get; }

    public CreateFileRepositoryDomainEvent(Guid fileRepositoryId, string fileRepositoryName, string fileRepositoryVersion, List<string> fileRepositoryTags)
    {
        FileRepositoryId = fileRepositoryId;
        FileRepositoryName = fileRepositoryName;
        FileRepositoryVersion = fileRepositoryVersion;
        FileRepositoryTags = fileRepositoryTags;
    }
}

