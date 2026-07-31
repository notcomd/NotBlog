
namespace FileDev.Domain.Events;

public class CreateFileGroupEvent(Guid fileId, Guid userId, string fileGroupName,
HashSet<string>? fileGroupTags = null,
    string? fileGroupDescription = null,
     FileIdentity fileIdentity = FileIdentity.FilePublic) : INotifications
{

    public Guid FileId { get; } = fileId;

    public Guid UserId { get; } = userId;

    public string FileGroupName { get; } = fileGroupName;

    public string FileName { get; } = fileGroupName;

    public string? FileDescription { get; } = fileGroupDescription;

    public FileIdentity FileIdentity { get; } = fileIdentity;

    // public FileType FileType { get; } = fileType;

    public HashSet<string>? FileGroupTags { get; } = fileGroupTags;

    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}