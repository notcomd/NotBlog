using System.Diagnostics;
using FileDev.Domain.Dto.Response;
using NotMediator;

namespace FileDev.Web.API.Application.Commands;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class CreateNotFileCommand(Guid userGuid,
                            string fileName,
                            Uri filePath,
                            string fileMd5,
                            Domain.Enum.FileIdentity fileIdentity,
                            long fileSize,
                            HashSet<string>? fileTags,
                            string? fileDescription
                           ) : IRequest<bool>
{
    public Guid UserGuid { get; set; } = userGuid;

    public Domain.Enum.FileIdentity FileIdentity { get; set; } = fileIdentity;

    public HashSet<string> FileTags { get; set; } = fileTags ?? [];

    public string FileDescription { get; set; } = fileDescription ?? string.Empty;

    public string FileName { get; set; } = fileName;

    public Uri FilePath { get; set; } = filePath;

    public string FileMd5 { get; set; } = fileMd5;

    public long FileSize { get; set; } = fileSize;

    /// <summary>存储层元数据（内容哈希 / 存储层 / 卷 / 分片数等），与 Lite 元数据对齐。</summary>
    public NotFileStorageResponse? StorageMeta { get; set; }

    // Major：删除原 Equals/GetHashCode/ToString 仅调用 base 的无意义重写
    private string GetDebuggerDisplay()
    {
        return $"{nameof(CreateNotFileCommand)}: UserGuid={UserGuid}, FileName={FileName}";
    }
}
