using System.Diagnostics;
using NotMediator;

namespace FileDev.Web.API.Application.Command;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class CreateNotFileCommand(Guid userGuid,
                            string fileName,
                            Uri filePath,
                            string fileMd5,
                            FileIdentity fileIdentity,
                             Uri fileUri,
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

    public Uri FileUri { get; set; } = fileUri;

    public long FileSize { get; set; } = fileSize;

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string? ToString()
    {
        return base.ToString();
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}