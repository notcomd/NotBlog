namespace FileDev.Domain.Entities;

public class File : IAggregateRoot
{

    private File() {}

    public Guid FildId { get; set; }

    public string FileName { get; set; } = null!;

    public List<string>? FileTitels { get; set; } = new();

    public string FileDescription { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public double FileSize { get; set; }

    public Uri FileUri { get; set; } = null!;
}