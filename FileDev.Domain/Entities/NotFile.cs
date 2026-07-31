namespace FileDev.Domain.Entities;

public class NotFile : Entity, IAggregateRoot
{
    public Guid FileId { get; init; }

    public Guid UserId { get; init; }

    public string FileName { get; private set; } = null!;

    public HashSet<string> FileTags { get; private set; }

    public string FileDescription { get; set; } = string.Empty;

    public long FileSize { get; private set; }

    public Uri FileUri { get; private set; } = null!;

    public string FileMd5 { get; private set; } = string.Empty;

    public FileIdentity FileIdentity { get; private set; }

    public DateTimeOffset UploadTime { get; init; }

    public DateTimeOffset UpdateTime { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeleteTime { get; private set; }

    public NotFile(Guid userId, string fileName, HashSet<string>? fileTags, string fileDescription,
           long fileSize, Uri fileUri, string fileMd5,
           FileIdentity fileIdentity = FileIdentity.FilePrivate) : this()
    {
        if (userId == Guid.Empty) throw new ArgumentNullException(nameof(userId), "用户ID不能为空");
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名不能为 null 或空白", nameof(fileName));
        if (fileSize < 0) throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");
        ArgumentNullException.ThrowIfNull(fileUri);

        UserId = userId;
        FileName = fileName;
        FileDescription = fileDescription;
        FileTags = fileTags ?? [];
        FileSize = fileSize;
        FileMd5 = fileMd5;
        FileUri = fileUri;
        FileIdentity = fileIdentity;
        /// 添加领域事件
        AddDomainEvent(new UploadNotFileEvent(this.FileId, this.UserId, this.FileName, this.FileTags,
            this.FileDescription, this.FileSize, this.FileUri, this.FileMd5, this.FileIdentity));
    }

    private NotFile()
    {
        FileId = Guid.CreateVersion7();
        UploadTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        FileTags = [];
        IsDeleted = false;
    }


    public void ChangeFileData(string? fileName, HashSet<string>? tags, string? fileDescription,
        FileIdentity? fileIdentity, string fileMd5)
    {
        if (string.IsNullOrEmpty(fileMd5))
            throw new ArgumentException("文件 MD5 不能为空", nameof(fileMd5));

        if (!string.IsNullOrWhiteSpace(fileName) && !FileName.Equals(fileName.Trim()))
            FileName = fileName.Trim();

        if (tags != null && (!FileTags.SetEquals(tags)))
            FileTags = [.. tags];

        if (!string.IsNullOrWhiteSpace(fileDescription) && !FileDescription.Equals(fileDescription.Trim()))
            FileDescription = fileDescription.Trim();

        if (fileIdentity.HasValue && (!FileIdentity.Equals(fileIdentity.Value)))
            FileIdentity = fileIdentity.Value;

        if (!FileMd5.Equals(fileMd5))
            FileMd5 = fileMd5;

        UpdateTime = DateTimeOffset.UtcNow;
        AddDomainEvent(new ChangeFileDataEvent(this.FileId, this.UserId, this.FileName, this.FileTags,
            fileDescription ?? this.FileDescription, this.FileIdentity, this.FileMd5));
    }

    public void SetFileMd5(string fileMd5)
    {
        FileMd5 = fileMd5;
    }

    public bool IsFileEqualMd5(string fileMd5)
    {
        return fileMd5 == FileMd5;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("标签不能为 null 或空白", nameof(tag));

        FileTags.Add(tag);
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public bool IsEquesFile(string fileMd5)
    {
        return fileMd5 == FileMd5;
    }

    public void RemoveTag(string tag)
    {
        if (FileTags.Remove(tag))
            UpdateTime = DateTimeOffset.UtcNow;
    }


    public void SoftDelete()
    {
        if (IsDeleted)
            throw new InvalidOperationException("文件已经被删除");

        IsDeleted = true;
        DeleteTime = DateTimeOffset.UtcNow;
        UpdateTime = DateTimeOffset.UtcNow;
        /// 添加领域事件
        AddDomainEvent(new DeleteFileEvent(this.FileId, this.UserId));
       }

    public void Restore()
    {
        if (!IsDeleted)
            throw new InvalidOperationException("文件没有被删除");
        IsDeleted = false;
        DeleteTime = null;
        UpdateTime = DateTimeOffset.UtcNow;
    }


    public class NotFileBuilder
    {
        private string _fileDescription = string.Empty;
        private FileIdentity _fileIdentity = FileIdentity.FilePrivate;
        private string _fileMd5 = string.Empty;
        private string _fileName = null!;
        private long _fileSize;
        private HashSet<string> _fileTags = [];
        private FileType _fileType;
        private Uri _fileUri = null!;
        private Guid _userId;

        public NotFileBuilder WithUserId(Guid userId)
        {
            _userId = userId;
            return this;
        }

        public NotFileBuilder WithFileName(string fileName)
        {
            _fileName = fileName;
            return this;
        }

        public NotFileBuilder WithFileTags(IEnumerable<string>? fileTags)
        {
            _fileTags = fileTags == null ? [] : [.. fileTags];
            return this;
        }

        public NotFileBuilder WithFileDescription(string fileDescription)
        {
            _fileDescription = fileDescription;
            return this;
        }

        public NotFileBuilder WithFileType(FileType fileType)
        {
            _fileType = fileType;
            return this;
        }

        public NotFileBuilder WithFileSize(long fileSize)
        {
            _fileSize = fileSize;
            return this;
        }

        public NotFileBuilder WithFileUri(Uri fileUri)
        {
            _fileUri = fileUri;
            return this;
        }

        public NotFileBuilder WithFileMd5(string fileMd5)
        {
            _fileMd5 = fileMd5;
            return this;
        }

        public NotFileBuilder WithFileIdentity(FileIdentity fileIdentity)
        {
            _fileIdentity = fileIdentity;
            return this;
        }

        public NotFile Build()
        {
            // 验证必需字段
            if (_userId == Guid.Empty)
                throw new InvalidOperationException("UserId must be provided and cannot be empty.");
            if (string.IsNullOrWhiteSpace(_fileName))
                throw new InvalidOperationException("FileName must be provided and cannot be empty.");
            if (_fileUri == null)
                throw new InvalidOperationException("FileUri must be provided.");

            return new NotFile(
                _userId,
                _fileName,
                _fileTags,
                _fileDescription,
                _fileSize,
                _fileUri,
                _fileMd5,
                _fileIdentity);
        }
    }
}