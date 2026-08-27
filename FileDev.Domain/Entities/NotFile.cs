namespace FileDev.Domain.Entities;

public class NotFile : Entity<Guid>, IAggregateRoot
{
    public Guid FileId { get; init; }

    public Guid UserId { get; init; }

    public string FileName { get; private set; } = null!;

    public List<string> FileTags { get; private set; }

    public string FileDescription { get; private set; } = string.Empty;

    public long FileSize { get; private set; }

    public Uri FileUri { get; private set; } = null!;

    public string FileMd5 { get; private set; } = string.Empty;

    public FileIdentity FileIdentity { get; private set; }

    /// <summary>文件来源域（用户仓库 / 内容附件），默认用户仓库。</summary>
    public FileSource Source { get; private set; } = FileSource.UserRepository;

    /// <summary>是否公开文件（FilePublic/FilePrivate）。</summary>
    public bool IsPublic => FileIdentity == FileIdentity.FilePublic;

    public DateTimeOffset UploadTime { get; init; }

    public DateTimeOffset UpdateTime { get; private set; }

    // ---- 与 MohuTianchi.Lite 对齐的存储元数据 ----

    /// <summary>内容 SHA-256 摘要（= Lite ObjectIndexEntry.ContentHash），用于去重比对与存储校验。</summary>
    public string ContentHash { get; private set; } = string.Empty;

    /// <summary>存储层（Hot/Cold），与 Lite StorageTier 对齐。</summary>
    public StorageTier Tier { get; private set; } = StorageTier.Hot;

    /// <summary>物理分片所在数据卷 ID（"v{n}" 或空串表示默认卷，与 Lite ObjectManifest.VolumeId 对齐）。</summary>
    public string VolumeId { get; private set; } = string.Empty;

    /// <summary>物理分片总数（Lite 内容寻址二次切片后的分片数，与 ObjectManifest.Shards 数量对齐）。</summary>
    public int ShardCount { get; private set; }

    /// <summary>TTL 过期时间（Lite ObjectManifest.ExpiresAt；为空表示永不过期）。</summary>
    public DateTimeOffset? StorageExpiresAt { get; private set; }

    /// <summary>Lite 清单最近更新 UTC 时间（ObjectManifest.UpdatedUtc）。</summary>
    public DateTimeOffset StorageUpdatedUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeleteTime { get; private set; }

    public NotFile(Guid userId, string fileName, HashSet<string>? fileTags, string fileDescription,
           long fileSize, Uri fileUri, string fileMd5,
           FileIdentity fileIdentity = FileIdentity.FilePrivate,
           FileSource source = FileSource.UserRepository) : this()
    {
        if (userId == Guid.Empty) throw new ArgumentNullException(nameof(userId), "用户ID不能为空");
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("文件名不能为 null 或空白", nameof(fileName));
        if (fileSize < 0) throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小不能为负数");
        ArgumentNullException.ThrowIfNull(fileUri);
        ArgumentNullException.ThrowIfNull(fileMd5);

        UserId = userId;
        FileName = fileName;
        FileDescription = fileDescription;
        FileTags = fileTags is null ? [] : [.. fileTags];
        FileSize = fileSize;
        FileMd5 = fileMd5;
        FileUri = fileUri;
        FileIdentity = fileIdentity;
        Source = source;
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
        ContentHash = string.Empty;
        Tier = StorageTier.Hot;
        VolumeId = string.Empty;
        StorageUpdatedUtc = DateTimeOffset.UtcNow;
    }


    public void ChangeFileData(string? fileName, HashSet<string>? tags, string? fileDescription,
        FileIdentity? fileIdentity, string fileMd5)
    {
        if (string.IsNullOrEmpty(fileMd5))
            throw new ArgumentException("文件 MD5 不能为空", nameof(fileMd5));

        if (!string.IsNullOrWhiteSpace(fileName) && !FileName.Equals(fileName.Trim()))
            FileName = fileName.Trim();

        if (tags != null && !new HashSet<string>(FileTags).SetEquals(tags))
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

    public bool IsFileEqualMd5(string fileMd5)
    {
        return fileMd5 == FileMd5;
    }

    /// <summary>
    /// 应用与 Lite 对齐的存储元数据（由文件创建链路在拿到存储结果后调用）。
    /// 物理分片所在卷/分片数/哈希/分层等仅在存储完成后可知。
    /// </summary>
    /// <param name="contentHash">内容 SHA-256 摘要。</param>
    /// <param name="tier">存储层（Hot/Cold）。</param>
    /// <param name="volumeId">物理分片所在数据卷 ID。</param>
    /// <param name="shardCount">物理分片总数。</param>
    /// <param name="expiresAt">TTL 过期时间（可为空）。</param>
    /// <param name="updatedUtc">Lite 清单更新时间。</param>
    public void ApplyStorageMeta(
        string contentHash, StorageTier tier, string volumeId, int shardCount,
        DateTimeOffset? expiresAt, DateTimeOffset updatedUtc)
    {
        ArgumentNullException.ThrowIfNull(contentHash);
        if (shardCount < 0)
            throw new ArgumentOutOfRangeException(nameof(shardCount), "分片总数不能为负数");

        ContentHash = contentHash;
        Tier = tier;
        VolumeId = volumeId ?? string.Empty;
        ShardCount = shardCount;
        StorageExpiresAt = expiresAt;
        StorageUpdatedUtc = updatedUtc;
        UpdateTime = DateTimeOffset.UtcNow;
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("标签不能为 null 或空白", nameof(tag));

        if (!FileTags.Contains(tag))
            FileTags.Add(tag);
        UpdateTime = DateTimeOffset.UtcNow;
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
        private string _contentHash = string.Empty;
        private StorageTier _tier = StorageTier.Hot;
        private string _volumeId = string.Empty;
        private int _shardCount;
        private DateTimeOffset? _expiresAt;
        private DateTimeOffset _storageUpdatedUtc = DateTimeOffset.UtcNow;
        private bool _hasStorageMeta;
        private string _fileName = null!;
        private long _fileSize;
        private HashSet<string> _fileTags = [];
        private Uri _fileUri = null!;
        private Guid _userId;
        private FileSource _source = FileSource.UserRepository;

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

        public NotFileBuilder WithSource(FileSource source)
        {
            _source = source;
            return this;
        }

        public NotFileBuilder WithStorageMeta(
            string contentHash, StorageTier tier, string volumeId, int shardCount,
            DateTimeOffset? expiresAt = null, DateTimeOffset? updatedUtc = null)
        {
            _contentHash = contentHash;
            _tier = tier;
            _volumeId = volumeId;
            _shardCount = shardCount;
            _expiresAt = expiresAt;
            if (updatedUtc.HasValue)
                _storageUpdatedUtc = updatedUtc.Value;
            _hasStorageMeta = true;
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

            var file = new NotFile(
                _userId,
                _fileName,
                _fileTags,
                _fileDescription,
                _fileSize,
                _fileUri,
                _fileMd5,
                _fileIdentity,
                _source);

            if (_hasStorageMeta)
                file.ApplyStorageMeta(_contentHash, _tier, _volumeId, _shardCount, _expiresAt, _storageUpdatedUtc);

            return file;
        }
    }
}