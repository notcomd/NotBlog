using Markdown.Domain.SeedWork;

namespace Markdown.Domain.Entities;

/// <summary>
///     MarkDown 阅读历史记录
/// </summary>
public class MarkHository : Entity, IAggregateRoot
{
    private MarkHository()
    {
        MarkHositoryGuid = Guid.CreateVersion7();
        ReadTime = DateTime.UtcNow;
        LastReadTime = DateTime.UtcNow;
        ReadingProgress = 0;
    }

    private MarkHository(Guid userGuid, 
        Guid markDownGuid, int readingProgress, string? note) : this()
    {
        UserGuid = userGuid;
        MarkDownGuid = markDownGuid;
        ReadingProgress = readingProgress;
        Note = note;
    }

    public Guid MarkHositoryGuid { get; init; }
    
    /// <summary>
    ///     用户 GUID
    /// </summary>
    public Guid UserGuid { get; init; }

    /// <summary>
    ///     MarkDown 文档 GUID
    /// </summary>
    public Guid MarkDownGuid { get; init; }

    /// <summary>
    ///     阅读进度 (0-100)
    /// </summary>
    public int ReadingProgress { get; private set; }

    /// <summary>
    ///     首次阅读时间
    /// </summary>
    public DateTime ReadTime { get; init; }

    /// <summary>
    ///     最后阅读时间
    /// </summary>
    public DateTime LastReadTime { get; private set; }

    /// <summary>
    ///     阅读笔记或备注
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    ///     阅读次数
    /// </summary>
    public int ReadCount { get; private set; } = 1;

    // 导航属性
    public MarkDown? MarkDown { get; private set; }

    public Task<MarkHository> UpdateReadingProgressAsync(int progress)
    {
        if (progress < 0 || progress > 100)
            throw new ArgumentOutOfRangeException(nameof(progress), "阅读进度必须在 0-100 之间");
        
        ReadingProgress = progress;
        LastReadTime = DateTime.UtcNow;
        return Task.FromResult(this);
    }

    public Task<MarkHository> AddReadCountAsync()
    {
        ReadCount++;
        LastReadTime = DateTime.UtcNow;
        return Task.FromResult(this);
    }

    public Task<MarkHository> UpdateNoteAsync(string? note)
    {
        Note = note;
        return Task.FromResult(this);
    }

    public static MarkHository Create(Guid userGuid, Guid markDownGuid, int readingProgress = 0, string? note = null)
    {
        return new MarkHository(userGuid, markDownGuid, readingProgress, note);
    }

    /// <summary>
    ///     MarkDown 阅读历史构建器
    /// </summary>
    public class Builder
    {
        private readonly Guid _userGuid;
        private readonly Guid _markDownGuid;
        private int _readingProgress;
        private string? _note;

        public Builder(Guid userGuid, Guid markDownGuid)
        {
            _userGuid = userGuid;
            _markDownGuid = markDownGuid;
        }

        public Builder WithReadingProgress(int progress)
        {
            if (progress < 0 || progress > 100)
                throw new ArgumentOutOfRangeException(nameof(progress), "阅读进度必须在 0-100 之间");
            
            _readingProgress = progress;
            return this;
        }

        public Builder WithNote(string? note)
        {
            _note = note;
            return this;
        }

        public MarkHository Build()
        {
            return new MarkHository(_userGuid, _markDownGuid, _readingProgress, _note);
        }
    }
}