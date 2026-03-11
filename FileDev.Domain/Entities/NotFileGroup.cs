using FileDev.Domain.SeedWork;

namespace FileDev.Domain.Entities;

public class NotFileGroup : Entity, IAggregateRoot
{
    public NotFileGroup()
    {
        NotFileGroupId = Guid.CreateVersion7();
        UploadTime = DateTime.Now;
        UpdateTime = DateTime.Now;
        IsDeleted = false;
        FileIdentity = FileIdentity.FilePublic;
        FileType = FileType.CompressFiles;
    }

    public NotFileGroup(Guid userId, string fileGroupName, HashSet<string>? fileGroupTags, string? fileGroupDescription,
        FileIdentity fileIdentity, FileType fileType) : this()
    {
        UserId = userId;
        FileGroupName = fileGroupName;
        FileGroupTags = fileGroupTags ?? new HashSet<string>();
        FileGroupDescription = fileGroupDescription;
        FileIdentity = fileIdentity;
        FileType = fileType;
    }

    public Guid NotFileGroupId { get; init; }

    public Guid UserId { get; init; }

    public string FileGroupName { get; private set; } = null!;

    public HashSet<string> FileGroupTags { get; private set; } = new();

    public HashSet<Guid> FileIds { get; } = new();

    public string? FileGroupDescription { get; private set; } = string.Empty;

    public DateTime UploadTime { get; init; }

    public DateTime UpdateTime { get; private set; }

    public bool IsDeleted { get; private set; }

    public FileIdentity FileIdentity { get; private set; }

    public FileType FileType { get; private set; }

    public void UpdateFileGroup(string fileGroupName, HashSet<string>? fileGroupTags, string? fileGroupDescription,
        FileIdentity fileIdentity, FileType fileType)
    {
        FileGroupName = fileGroupName;
        FileGroupTags = fileGroupTags ?? new HashSet<string>();
        FileGroupDescription = fileGroupDescription;
        FileIdentity = fileIdentity;
        FileType = fileType;
        UpdateTime = DateTime.Now;
    }

    public void AddFile(Guid fileId)
    {
        FileIds.Add(fileId);
    }

    public void RemoveFile(Guid fileId)
    {
        FileIds.Remove(fileId);
    }

    public void Delete()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
    }

    public void AddTag(string tag)
    {
        FileGroupTags.Add(tag);
    }

    public class NotFileGroupBuilder
    {
        private string? _fileGroupDescription;
        private string _fileGroupName = null!;
        private HashSet<string>? _fileGroupTags;
        private FileIdentity _fileIdentity;
        private FileType _fileType;
        private Guid _userId;

        public NotFileGroupBuilder WithUserId(Guid userId)
        {
            _userId = userId;
            return this;
        }

        public NotFileGroupBuilder WithFileGroupName(string fileGroupName)
        {
            _fileGroupName = fileGroupName;
            return this;
        }

        public NotFileGroupBuilder WithFileGroupTags(IEnumerable<string>? fileGroupTags)
        {
            _fileGroupTags = fileGroupTags?.ToHashSet();
            return this;
        }

        public NotFileGroupBuilder WithFileGroupDescription(string fileGroupDescription)
        {
            _fileGroupDescription = fileGroupDescription;
            return this;
        }

        public NotFileGroupBuilder WithFileIdentity(FileIdentity fileIdentity)
        {
            _fileIdentity = fileIdentity;
            return this;
        }

        public NotFileGroupBuilder WithFileType(FileType fileType)
        {
            _fileType = fileType;
            return this;
        }

        public NotFileGroup Build()
        {
            return new NotFileGroup(_userId, _fileGroupName, _fileGroupTags, _fileGroupDescription, _fileIdentity,
                _fileType);
        }
    }
}