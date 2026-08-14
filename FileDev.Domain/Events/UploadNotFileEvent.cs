using System.Diagnostics;
using NotMediator;

namespace FileDev.Domain.Events;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public record UploadNotFileEvent(Guid FileId,
                                 Guid UserId,
                                 string FileName,
                                 List<string>? FileTags,
                                 string FileDescription,
                                 long FileSize,
                                 Uri FileUri,
                                 string FileMd5,
                                 FileIdentity FileIdentity)
    : INotifications
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}
