using NotMediator;

namespace FileDev.Web.API.ActionFilter.Command;

public class CreateNotFileCommand : IRequest<bool>
{
    public CreateNotFileCommand(Guid userGuid, string fileName, Uri filePath, string fileMd5, Uri fileUri,
        long fileSize)
    {
        UserGuid = userGuid;
        FileName = fileName;
        FilePath = filePath;
        FileMd5 = fileMd5;
        FileUri = fileUri;
        FileSize = fileSize;
    }

    public Guid UserGuid { get; set; }

    public string FileName { get; set; }

    public Uri FilePath { get; set; }

    public string FileMd5 { get; set; }

    public Uri FileUri { get; set; }

    public long FileSize { get; set; }
}