using NotMediator;

namespace FileDev.Web.API.Application.Command;

public class CreateNotFileCommand(Guid userGuid,
                            string fileName,
                            Uri filePath,
                            string fileMd5,
                            Uri fileUri,
                            long fileSize) : IRequest<bool>
{
    public Guid UserGuid { get; set; } = userGuid;

    public string FileName { get; set; } = fileName;

    public Uri FilePath { get; set; } = filePath;

    public string FileMd5 { get; set; } = fileMd5;

    public Uri FileUri { get; set; } = fileUri;

    public long FileSize { get; set; } = fileSize;
}