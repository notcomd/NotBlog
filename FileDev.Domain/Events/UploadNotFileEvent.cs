
using System.Diagnostics;
using NotMediator;

namespace FileDev.Domain.Events;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public record UploadNotFileEvent(NotFile NotFile,
                                 Guid UserGuid,
                                 string FileName,
                                 Uri FilePath,
                                 long FileSize,
                                 string FileMd5,
                                 FileIdentity FileIdentity
                                 )
    : INotifications
{
    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}