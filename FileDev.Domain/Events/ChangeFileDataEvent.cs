using System.Diagnostics;
using NotMediator;

namespace FileDev.Domain.Events;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class ChangeFileDataEvent(Guid fileId,
                                 Guid userId,
                                 string fileName,
                                 HashSet<string> fileTags,
                                 string fileDescription,
                                 FileIdentity fileIdentity,
                                 string fileMd5) :INotifications
{
    public Guid FileId { get; } = fileId;
    public Guid UserId { get; } = userId;
    public string FileName { get; init; } = fileName;
    public HashSet<string> FileTags { get; init; } = fileTags;
    public string FileDescription { get; init; } = fileDescription;
    public FileIdentity FileIdentity { get; init; } = fileIdentity;
    public string FileMd5 { get; init; } = fileMd5;
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;

    private string GetDebuggerDisplay() => this.ToString() ?? "";
}