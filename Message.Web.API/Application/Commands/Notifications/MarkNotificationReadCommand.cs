namespace Message.Web.API.Application.Commands.Notifications;

/// <summary>标记单条通知已读命令。</summary>
public record MarkNotificationReadCommand(Guid NotifyGuid, Guid UserId) : IRequest<bool>;
