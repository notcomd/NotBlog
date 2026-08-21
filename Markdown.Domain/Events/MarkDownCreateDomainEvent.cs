namespace Markdown.Domain.Events;

public record MarkDownCreateDomainEvent(Guid MarkdownGuid,Guid UserGuid,string MarkdownTitle,DateTimeOffset DateTimeOffset) : INotifications
{
    public DateTimeOffset CreateAt=>DateTimeOffset.UtcNow;
}