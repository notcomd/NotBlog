namespace Markdown.Domain.Entities;

public class MarkDown : IAggregateRoot
{
    public Guid MarkDownGuid { get; init; }
    public string MarkDownName { get; set; } = null!;
}