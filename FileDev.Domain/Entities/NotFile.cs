using DomainCommonst;

namespace FileDev.Domain.Entities;

public class NotFile : Entity, IAggregateRoot
{

    public Guid UserGuid { get; private set; }

    public string FileName { get; private set; } = null!;

    public string FileDescription { get; private set; } = null!;

    public string FileType { get; private set; } = null!;

    public double FileSize { get; private set; }

    public FileSafety FileSafety { get; private set; }

    public string FileHash { get; private set; }

    public Uri FileUri { get; private set; } = null!;


    private NotFile() { }

    public NotFile(Guid userGuid, string fileName, string fileDescription, string fileType,
        double fileSize, FileSafety fileIdentity,
        string fileHash, Uri fileUri)
    {
        Id = Guid.CreateVersion7();
        UserGuid = userGuid;
        FileName = fileName;
        FileDescription = fileDescription;
        FileType = fileType;
        FileSize = fileSize;
        FileSafety = fileIdentity;
        FileHash = fileHash;
        FileUri = fileUri;
    }
}