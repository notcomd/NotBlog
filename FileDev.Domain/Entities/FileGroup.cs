using System.Collections.ObjectModel;

using DomainCommonst;

namespace FileDev.Domain.Entities;

public class FileGroup : Entity, IAggregateRoot
{

    public Guid UserGuid { get; private set; }

    public string FileGroupName { get; private set; } = null!;

    public FileSafety FileSafety { get; private set; }

    public DateTime CreateTime { get; init; }


    private List<string> _fileGroupTags = new List<string>();
    public ReadOnlyCollection<string> FileGroupTags => _fileGroupTags.AsReadOnly();

    private List<Guid> _files = new List<Guid>();
    public ReadOnlyCollection<Guid> Files => _files.AsReadOnly();

    private List<FileGroup> _childrenFileGroup = new List<FileGroup>();
    public ReadOnlyCollection<FileGroup> ChlidrenFileGroup => _childrenFileGroup.AsReadOnly();

    protected FileGroup() { }

    public FileGroup(Guid userGuid, string fileGroupName, FileSafety fileSafety, DateTime createTime)
    {
        UserGuid = userGuid;
        FileGroupName = fileGroupName;
        FileSafety = fileSafety;
        CreateTime = createTime;
    }




}
