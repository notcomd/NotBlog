namespace Markdown.Domain.Entities;

/// <summary>
///     文档
/// </summary>
public class MarkDown : IAggregateRoot
{

    public Guid MarkDownGuid { get; init; }

    public Guid MarkReviewGuid { get; init; }
    
    public Guid MarkUserGuid { get; init; }
    
    public string MarkDownName { get; private set; } = null!;

    public List<string>? MarkDownTagboard { get; private set; } = new();

    public MarkOption MarkOption { get; private set; } = MarkOption.Default;

    public string MarkDownHash { get; private set; }

    public DateTime CreateAt { get; init; }

    public string MarkDownContent { get; private set; }

    public DateTime UplaodAt { get; private set; }
    ///关系外键
    public ICollection<MarkReview> MarkReview { get; }

    private MarkDown()
    {
        this.MarkReview = new List<MarkReview>();
        this.MarkDownGuid = Guid.CreateVersion7();
        //this.
        this.CreateAt = DateTime.UtcNow;
        this.UplaodAt = DateTime.UtcNow;
    }

    public MarkDown(MarkDown markDown) : this()
    {
        MarkDownGuid = markDown.MarkDownGuid;
        MarkReview = new HashSet<MarkReview>();
    }

    public Task<MarkDown> AddByMarkReviewAsync(MarkReview markReview)
    {
        MarkReview.Add(markReview);
        return Task.FromResult(this);
    }
}