using System.Security.Cryptography.X509Certificates;

using DomainCommonst;

namespace FileDev.Domain.DomainEntities;

public class NotFile : Entity
{

    public Guid FileGroupGuid { get; private set; }

    public Guid UserGuid { get; private set; }

    public string FileName { get; private set; } = null!;

    public string FileDescription { get; private set; } = null!;

    public string FileType { get; private set; } = null!;

    public double FileSize { get; private set; }

    public FileSafety FileSafety { get; private set; }

    public string FileHash { get; private set; }


    public ObjectMap ObjectMap { get; private set; }

    private NotFile() { }

    public NotFile(Guid userGuid, string fileName, string fileDescription, string fileType,
        double fileSize, FileSafety fileIdentity,
        string fileHash, ObjectMap objectMap)
    {
        Id = Guid.CreateVersion7();
        UserGuid = userGuid;
        FileName = fileName;
        FileDescription = fileDescription;
        FileType = fileType;
        FileSize = fileSize;
        FileSafety = fileIdentity;
        FileHash = fileHash;
        ObjectMap = objectMap;
    }

    public void ReFileName(string fileName)
    {
        if (string.Equals(fileName, FileName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        FileName = fileName.Trim();
    }

}