namespace Markdown.Web.API.Application.DomainEventHandlers;

/// <summary>
///     Markdown 文档创建领域事件处理器
/// </summary>
public class MarkdownCreatedDomainEventHandler : INotificationHandler<MarkdownCreatedDomainEvent>
{
    public async Task Handler(MarkdownCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // TODO: 处理领域事件的业务逻辑
        // 例如：验证业务规则、发送领域通知等

        Console.WriteLine($"领域事件处理：Markdown 文档已创建 - {notification.MarkDownGuid}");

        await Task.CompletedTask;
    }
}

/// <summary>
///     Markdown 文档创建领域事件
/// </summary>
public record MarkdownCreatedDomainEvent(
    Guid MarkDownGuid,
    Guid MarkUserGuid,
    string FileName,
    DateTime CreatedAt
) : INotifications;