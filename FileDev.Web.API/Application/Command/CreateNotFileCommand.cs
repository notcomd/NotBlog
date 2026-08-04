using System.Diagnostics;
using NotMediator;

namespace FileDev.Web.API.Application.Command;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class CreateNotFileCommand(Guid userGuid,
                            string fileName,
                            Uri filePath,
                            string fileMd5,
                            FileIdentity fileIdentity,
                            long fileSize,
                            HashSet<string>? fileTags,
                            string? fileDescription
                           ) : IRequest<bool>
{
    public Guid UserGuid { get; set; } = userGuid;

    public FileIdentity FileIdentity { get; set; } = fileIdentity;

    public HashSet<string> FileTags { get; set; } = fileTags ?? [];

    public string FileDescription { get; set; } = fileDescription ?? string.Empty;

    public string FileName { get; set; } = fileName;

    public Uri FilePath { get; set; } = filePath;

    public string FileMd5 { get; set; } = fileMd5;

    public long FileSize { get; set; } = fileSize;

    // Major：删除原 Equals/GetHashCode/ToString 仅调用 base 的无意义重写
    private string GetDebuggerDisplay()
    {
        return $"{nameof(CreateNotFileCommand)}: UserGuid={UserGuid}, FileName={FileName}";
    }
}
